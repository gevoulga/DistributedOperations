using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Distops.Core.Model;
using Distops.Core.Services;
using Microsoft.CorrelationVector;

namespace Distops.ServiceBus.Services;

// https://docs.microsoft.com/en-us/samples/azure/azure-sdk-for-net/azuremessagingservicebus-samples/
// https://andrewlock.net/introducing-ihostlifetime-and-untangling-the-generic-host-startup-interactions/
public class ServiceBusDistopExecutor : IDistopService, IHostLifetime, IAsyncDisposable
{
    private const string CloudEventSource = "/cloudevents/distops/servicebus";
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private ServiceBusProcessor _serviceBusProcessor;
    private ILogger<ServiceBusDistopClient> _logger;

    public ServiceBusDistopExecutor(
        string connectionString,
        string topicName,
        string subscriptionName,
        ILogger<ServiceBusDistopClient> logger)
    {
        _logger = logger;
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(topicName);
        var options = new ServiceBusProcessorOptions
        {
            // By default or when AutoCompleteMessages is set to true, the processor will complete the message after executing the message handler
            // Set AutoCompleteMessages to false to [settle messages](https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement#peeklock) on your own.
            // In both cases, if the message handler throws an exception without settling the message, the processor will abandon the message.
            // AutoCompleteMessages = false,

            // I can also allow for multi-threading
            MaxConcurrentCalls = 2
        };


        // create a session processor that we can use to process the messages
        _serviceBusProcessor = _client.CreateProcessor(topicName, subscriptionName, options);

        // configure the message and error handler to use
        _serviceBusProcessor.ProcessMessageAsync += MessageHandler;
        _serviceBusProcessor.ProcessErrorAsync += ErrorHandler;
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
        await _serviceBusProcessor.DisposeAsync();
    }

    public async Task<object?> Call(DistopContext distopContext, CancellationToken? cancellationToken = default)
    {
        // Send a message in the topic
        await SendMessageAsync(distopContext, true);

        await using var acceptSessionAsync = await _client.AcceptSessionAsync("topicName", "subscriptionName", "sesssionId", cancellationToken: cancellationToken);
        var receiveMessage = await acceptSessionAsync.ReceiveMessageAsync(TimeSpan.FromSeconds(30), cancellationToken);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Deserialize the message body into a CloudEvent
            CloudEvent receivedCloudEvent = CloudEvent.Parse(receiveMessage.Body);
            var distopReturnedValue = receivedCloudEvent.Data.ToObjectFromJson<DistopReturnedValue>();

            // we can evaluate application logic and use that to determine how to settle the message.
            await acceptSessionAsync.CompleteMessageAsync(receiveMessage, cancellationToken);

            return distopReturnedValue.Value;
        }
        catch (DistopPermanentException distopPermanentException)
        {
            await acceptSessionAsync.DeadLetterMessageAsync(
                receiveMessage,
                nameof(DistopPermanentException),
                distopPermanentException.Message,
                cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            // this.telemetryLogger.Log(new MessagePostMetric(this.topicName, message, stopwatch.Elapsed));
        }
    }

    public async Task FireAndForget(DistopContext distopContext, CancellationToken? cancellationToken = default)
    {
        await SendMessageAsync(distopContext, false);
    }

    private async Task SendMessageAsync(
        DistopContext distopContext,
        bool expectResponse,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var rawMessage = BuildServiceBusMessage(distopContext, new CorrelationVector(), expectResponse);

        try
        {
            // _logger.LogInformation(
            //     "Posting ServiceBus message {0}/{1} to topic {2}, cv={3}",
            //     rawMessage.MessageId,
            //     this.topicName,
            //     this.operationContextProvider.Context?.CorrelationVector.Value);

            await this._sender.SendMessageAsync(rawMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            // this.logger.LogError(
            //     "Posting ServiceBus message {0}/{1} to topic {2} failed, cv={3}\r\n{4}",
            //     message.GetType().Name,
            //     rawMessage.MessageId,
            //     this.topicName,
            //     this.operationContextProvider.Context?.CorrelationVector.Value,
            //     ex);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            // this.telemetryLogger.Log(new MessagePostMetric(this.topicName, message, stopwatch.Elapsed));
        }
    }

    private ServiceBusMessage BuildServiceBusMessage(
        DistopContext distopContext,
        CorrelationVector cv,
        bool expectResponse)
    {
        var cloudEvent = new CloudEvent(
            CloudEventSource,
            nameof(DistopContext),
            distopContext);
        ServiceBusMessage message = new ServiceBusMessage(new BinaryData(cloudEvent))
        {
            ReplyToSessionId = "" // Generate a session id
        };

        // var args = JsonSerializer.Serialize(invocation.Arguments);
        // var genArgs = JsonSerializer.Serialize(invocation.GenericArguments);
        var serialized = JsonSerializer.Serialize(distopContext);
        // var serialized = JsonConvert.SerializeObject(distopContext);
        var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(serialized));
        serviceBusMessage.TimeToLive = TimeSpan.FromMinutes(10);
        serviceBusMessage.MessageId = Guid.NewGuid().ToString();
        serviceBusMessage.CorrelationId = cv.Value;
        serviceBusMessage.ApplicationProperties.Add("expect-reply", expectResponse);
        // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.SerializationMasterVersion, CurrentSerializationMasterVersion);
        // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.SerializationSubVersion, this.SerializationSubVersion);
        // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.MessageType, this.MessageType);
        // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.PostTime, DateTime.UtcNow);
        // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.TargetPool, currentPool);

        return serviceBusMessage;
    }

    public async Task WaitForStartAsync(CancellationToken cancellationToken) =>
        await _serviceBusProcessor.StartProcessingAsync(cancellationToken);

    public async Task StopAsync(CancellationToken cancellationToken) =>
        await _serviceBusProcessor.StopProcessingAsync(cancellationToken);

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Deserialize the message body into a CloudEvent
            CloudEvent receivedCloudEvent = CloudEvent.Parse(args.Message.Body);
            var distopContext = receivedCloudEvent.Data.ToObjectFromJson<DistopContext>();


            // we can evaluate application logic and use that to determine how to settle the message.
            await args.CompleteMessageAsync(args.Message);
        }
        catch (DistopPermanentException distopPermanentException)
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                nameof(DistopPermanentException),
                distopPermanentException.Message,
                args.CancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            // this.telemetryLogger.Log(new MessagePostMetric(this.topicName, message, stopwatch.Elapsed));
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Error on processing ServiceBus message:\r\nError Source={}\n\nNamespace={}\r\nEntityPath={}\r\n{}",
            args.ErrorSource,
            args.FullyQualifiedNamespace,
            args.EntityPath,
            args.Exception);
        return Task.CompletedTask;
    }
}