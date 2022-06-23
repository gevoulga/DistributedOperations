﻿using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Distops.Core.Model;
using Distops.Core.Services;
using Distops.ServiceBus.Options;
using Microsoft.CorrelationVector;
using Microsoft.Extensions.Options;

namespace Distops.ServiceBus.Services;

// https://docs.microsoft.com/en-us/samples/azure/azure-sdk-for-net/azuremessagingservicebus-samples/
// https://andrewlock.net/introducing-ihostlifetime-and-untangling-the-generic-host-startup-interactions/
public class ServiceBusDistopClient : IDistopService, IAsyncDisposable
{
    private const string CloudEventSource = "/cloudevents/distops/servicebus";

    private readonly ILogger<ServiceBusDistopClient> _logger;
    private readonly ServiceBusDistopClientOptions _options;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public ServiceBusDistopClient(
        IOptions<ServiceBusDistopClientOptions> options,
        ILogger<ServiceBusDistopClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _client = new ServiceBusClient(_options.ConnectionString, _options.ServiceBusClientOptions);
        _sender = _client.CreateSender(_options.TopicName);
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(
            _sender.DisposeAsync().AsTask(),
            _client.DisposeAsync().AsTask());
    }

    public async Task<object?> Call(DistopContext distopContext, CancellationToken? cancellationToken = default)
    {
        var messageId = await SendMessageAsync(distopContext, true, cancellationToken.GetValueOrDefault());
        return await ReceiveMessage(messageId, cancellationToken.GetValueOrDefault());
    }

    public async Task FireAndForget(DistopContext distopContext, CancellationToken? cancellationToken = default)
    {
        await SendMessageAsync(distopContext, false, cancellationToken.GetValueOrDefault());
    }

    // TODO use Polly
    private async Task<string> SendMessageAsync(
        DistopContext distopContext,
        bool expectResponse,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var message = BuildServiceBusMessage(distopContext, new CorrelationVector(), expectResponse);

        try
        {
            _logger.LogTrace("Posting ServiceBus message {} to {}", message.MessageId, _sender.EntityPath);
            await this._sender.SendMessageAsync(message, cancellationToken);
            return message.MessageId;
        }
        catch (ServiceBusException ex)
        {
            _logger.LogError(ex, "Posting ServiceBus message {} to {} failed", message.MessageId, _sender.EntityPath);
            throw ex.IsTransient ? new DistopTransientException(ex.Message, ex) : new DistopPermanentException(ex.Message, ex);
        }
        finally
        {
            stopwatch.Stop();
            // TODO add telemetry metrics
            // this.telemetryLogger.Log(new MessagePostMetric(this.topicName, message, stopwatch.Elapsed));
        }
    }

    private ServiceBusMessage BuildServiceBusMessage(
        DistopContext distopContext,
        CorrelationVector cv,
        bool expectResponse)
    {
        // var serialized = JsonSerializer.Serialize(distopContext);
        // var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(serialized));
        var cloudEvent = new CloudEvent(
            CloudEventSource,
            nameof(DistopContext),
            distopContext);
        var messageId = Guid.NewGuid().ToString();
        return new ServiceBusMessage(new BinaryData(cloudEvent))
        {
            // Return the result of the distop in a unique session of the sender
            ReplyToSessionId = expectResponse ? _options.InstanceName : null,
            // TODO Dedup on ServiceBus incorrect retries (sender fails before is aware of message sent -2 messages sent)
            // TODO The MessageId needs to be set explicitly and predictably
            MessageId = messageId,
            TimeToLive = TimeSpan.FromMinutes(10),
            CorrelationId = cv.Value,
            ApplicationProperties =
            {
                { "expect-reply", expectResponse }
            }
        };
        // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.SerializationMasterVersion, CurrentSerializationMasterVersion);
        // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.SerializationSubVersion, this.SerializationSubVersion);
        // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.MessageType, this.MessageType);
        // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.PostTime, DateTime.UtcNow);
        // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.TargetPool, currentPool);
    }

    // TODO Use Polly
    private async Task<object?> ReceiveMessage(string messageId, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        await using var acceptSessionAsync = await _client.AcceptSessionAsync(_options.TopicName, _options.SubscriptionName, _options.InstanceName, _options.ServiceBusSessionReceiverOptions, cancellationToken);
        var receiveMessage = await acceptSessionAsync.ReceiveMessageAsync(_options.ReceiveTimeout, cancellationToken);

        // TODO review whether we can explicitly receive a message with a specific correlationId
        // ideally if there are any pending messages from previous execution (where the client died without ever receiving the response)
        // we want to let these messages in the topic
        // if the client retries the operation (where ServiceBus dedup will drop the message), the distop result will be there to be collected
        // Make sure we receive a response for the message we have sent
        while (receiveMessage.CorrelationId != messageId)
        {
            // TODO review - Dead letter or Abandon?
            await acceptSessionAsync.AbandonMessageAsync(receiveMessage, null , cancellationToken);
            receiveMessage = await acceptSessionAsync.ReceiveMessageAsync(_options.ReceiveTimeout, cancellationToken);
        }

        try
        {
            // Deserialize the message body into a CloudEvent
            CloudEvent? receivedCloudEvent = CloudEvent.Parse(receiveMessage.Body);
            var distopReturnedValue = receivedCloudEvent?.Data?.ToObjectFromJson<DistopReturnedValue>();

            // we can evaluate application logic and use that to determine how to settle the message.
            await acceptSessionAsync.CompleteMessageAsync(receiveMessage, cancellationToken);
            return distopReturnedValue?.Value;
        }
        catch (ServiceBusException serviceBusException)
        {
            if (serviceBusException.IsTransient)
            {
                await acceptSessionAsync.DeadLetterMessageAsync(
                    receiveMessage,
                    nameof(DistopPermanentException),
                    serviceBusException.Message,
                    cancellationToken);
            }
            throw;
        }
        finally
        {
            stopwatch.Stop();
            // this.telemetryLogger.Log(new MessagePostMetric(this.topicName, message, stopwatch.Elapsed));
        }
    }
}