using System.Diagnostics;
using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Distops.Core.Model;
using Distops.Core.Services;
using Distops.ServiceBus.Options;
using Distops.ServiceBus.Utils;
using Microsoft.CorrelationVector;
using Microsoft.Extensions.Options;
using static Distops.ServiceBus.Services.ServiceBusDistopClient;

namespace Distops.ServiceBus.Services;

// https://docs.microsoft.com/en-us/samples/azure/azure-sdk-for-net/azuremessagingservicebus-samples/
// https://andrewlock.net/introducing-ihostlifetime-and-untangling-the-generic-host-startup-interactions/
public class ServiceBusDistopExecutor : IHostLifetime, IAsyncDisposable
{
    private readonly ILogger<ServiceBusDistopExecutor> _logger;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSessionProcessor _serviceBusProcessor;
    private readonly IDistopExecutor _distopExecutor;
    private readonly ServiceBusSender _sender;

    public ServiceBusDistopExecutor(
        IOptions<ServiceBusDistopOptions> options,
        ILogger<ServiceBusDistopExecutor> logger,
        IDistopExecutor distopExecutor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _distopExecutor = distopExecutor ?? throw new ArgumentNullException(nameof(distopExecutor));
        var optionsValue = options.Value ?? throw new ArgumentNullException(nameof(options));
        _client = new ServiceBusClient(optionsValue.ServiceBusEndpoint, optionsValue.ServiceBusClientOptions);
        _sender = _client.CreateSender(optionsValue.ResultTopic);
        _serviceBusProcessor = CreateProcessor(optionsValue);
    }

    public ServiceBusDistopExecutor(
        ILogger<ServiceBusDistopExecutor> logger,
        IOptions<ServiceBusDistopOptions> options,
        ServiceBusClient serviceBusClient,
        IDistopExecutor distopExecutor)
    {
        if (distopExecutor == null) throw new ArgumentNullException(nameof(distopExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var optionsValue = options.Value ?? throw new ArgumentNullException(nameof(options));
        _client = serviceBusClient;
        _distopExecutor = distopExecutor;
        _sender = _client.CreateSender(optionsValue.ResultTopic);
        _serviceBusProcessor = CreateProcessor(optionsValue);
    }

    private ServiceBusSessionProcessor CreateProcessor(ServiceBusDistopOptions options)
    {
        // create a session processor that we can use to process the messages
        var processor = _client.CreateSessionProcessor(options.ScheduleTopic, options.ScheduleSubscription, options.ServiceBusSessionProcessorOptions);
        // configure the message and error handler to use
        processor.ProcessMessageAsync += MessageHandler;
        processor.ProcessErrorAsync += ErrorHandler;
        return processor;
    }

    public async ValueTask DisposeAsync()
    {
        // TODO  cleanup the servicebus client if it was created in constructed (and not injected)
        await Task.WhenAll(
            _sender.DisposeAsync().AsTask(),
            _serviceBusProcessor.DisposeAsync().AsTask(),
            _client.DisposeAsync().AsTask());
    }

    public async Task WaitForStartAsync(CancellationToken cancellationToken) =>
        await _serviceBusProcessor.StartProcessingAsync(cancellationToken);

    public async Task StopAsync(CancellationToken cancellationToken) =>
        await _serviceBusProcessor.StopProcessingAsync(cancellationToken);

    private async Task MessageHandler(ProcessSessionMessageEventArgs args)
    {
        var watch = Stopwatch.StartNew();
        _logger.LogTrace("Received ServiceBus message {} on {}", args.Message.MessageId, _serviceBusProcessor.EntityPath);
        try
        {
            await ScheduleDistop(args);

            // we can evaluate application logic and use that to determine how to settle the message.
            await args.CompleteMessageAsync(args.Message);
        }
        catch (ServiceBusException serviceBusException)
        {
            _logger.LogError(serviceBusException, "Receiving ServiceBus message {} on {} failed", args.Message.MessageId, _serviceBusProcessor.EntityPath);
            if (!serviceBusException.IsTransient)
            {
                await args.DeadLetterMessageAsync(
                    args.Message,
                    nameof(DistopPermanentException),
                    serviceBusException.Message,
                    args.CancellationToken);
            }
            throw;
        }
        finally
        {
            watch.Stop();
            _logger.LogTrace("Received ServiceBus message {} to {} in {}", args.Message.MessageId, _serviceBusProcessor.EntityPath, watch.Elapsed);
            // TODO add telemetry metrics
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

    private async Task ScheduleDistop(ProcessSessionMessageEventArgs args)
    {
        // Check if we should send back a reply or not
        var responseInfo = ResponseInfo.FromProcessor(args);
        // Deserialize the Distop from a CloudEvent
        var distopContext = await args.Parse<DistopContext>();

        //TODO run the job and then complete the message?
        Task.Factory.StartNew(() => RunDistop(distopContext, responseInfo));
    }

    private async Task RunDistop(DistopContext distopContext, ResponseInfo responseInfo)
    {
        var watch = Stopwatch.StartNew();
        //Or just accept and then fire it off?
        _logger.LogInformation("Distop started: '{}'", distopContext);
        var result = await _distopExecutor.ExecuteDistop(distopContext);
        _logger.LogInformation("Distop finished: '{}', elapsed '{}'", distopContext, watch.Elapsed);

        if (responseInfo.ExpectReply)
        {
            var message = BuildServiceBusMessage(result, responseInfo);
            await SendDistopResult(message, CancellationToken.None); //TODO review cancellation token?
        }
    }

    // TODO use Polly
    private async Task SendDistopResult(ServiceBusMessage message, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogTrace("Posting ServiceBus message {} to {}", message.MessageId, _sender.EntityPath);
            await _sender.SendMessageAsync(message, cancellationToken);
        }
        catch (ServiceBusException ex)
        {
            _logger.LogError(ex, "Posting ServiceBus message {} to {} failed", message.MessageId, _sender.EntityPath);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogTrace("Posted ServiceBus message {} to {} in {}", message.MessageId, _sender.EntityPath, stopwatch.Elapsed);
            // TODO add telemetry metrics
            // this.telemetryLogger.Log(new MessagePostMetric(this.topicName, message, stopwatch.Elapsed));
        }

    }

    private ServiceBusMessage BuildServiceBusMessage(Result<object?, Exception> result, ResponseInfo responseInfo)
    {
        var distopReturnedValue = result.ExtractError(out var res, out var exx)
            ? new DistopReturnedValue {Value = exx}
            : new DistopReturnedValue {Value = res}; // TODO returning exceptions

        return new ServiceBusMessage(distopReturnedValue.ToCloudEvent())
        {
            // Return the result of the distop in a unique session of the sender
            SessionId = responseInfo.SessionId,
            // TODO Dedup on ServiceBus incorrect retries (sender fails before is aware of message sent -2 messages sent)
            // TODO The MessageId needs to be set explicitly and predictably
            MessageId = Guid.NewGuid().ToString(),
            TimeToLive = TimeSpan.FromMinutes(10),
            CorrelationId = responseInfo.MessageId
        };
        // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.SerializationMasterVersion, CurrentSerializationMasterVersion);
        // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.SerializationSubVersion, this.SerializationSubVersion);
        // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.MessageType, this.MessageType);
        // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.PostTime, DateTime.UtcNow);
        // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.TargetPool, currentPool);
    }

    private record ResponseInfo
    {
        public bool ExpectReply { get; private init; }
        public string SessionId { get; private init; }
        public string MessageId { get; private init; }

        internal static ResponseInfo FromProcessor(ProcessSessionMessageEventArgs args)
        {
            // TODO review: reply to sessionId or check the custom header?
            // args.Message.ReplyToSessionId;
            return new ResponseInfo()
            {
                ExpectReply = args.Message.ApplicationProperties.TryGetValue(ExpectReplyHeader, out var reply) && (reply as bool? ?? false),
                SessionId = args.Message.ReplyToSessionId,
                MessageId = args.Message.MessageId
            };
        }
    }
}