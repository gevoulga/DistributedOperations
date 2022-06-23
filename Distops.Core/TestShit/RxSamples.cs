using Azure.Messaging.ServiceBus;

namespace Distops.Core.TestShit;

// https://docs.microsoft.com/en-us/samples/azure/azure-sdk-for-net/azuremessagingservicebus-samples/
public class RxSamples
{
    private const string CloudEventSource = "/cloudevents/distops/servicebus";
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private ILogger<ServiceBusDistopService> _logger;
    public RxSamples(string connectionString, string topicName)
    {
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(topicName);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }

    // public async Task<object?> Call(DistopContext distopContext)
    // {
    //     // Send a message in the topic
    //     await SendMessageAsync(distopContext, true);
    //
    //     throw new NotImplementedException();
    // }
    //
    // public async Task FireAndForget(DistopContext distopContext)
    // {
    //     await SendMessageAsync(distopContext, false);
    // }
    //
    // private async Task SendMessageAsync(
    //     DistopContext distopContext,
    //     bool expectResponse,
    //     CancellationToken cancellationToken = default)
    // {
    //     var stopwatch = Stopwatch.StartNew();
    //     var rawMessage = BuildServiceBusMessage(distopContext, new CorrelationVector(), expectResponse);
    //
    //     try
    //     {
    //         // _logger.LogInformation(
    //         //     "Posting ServiceBus message {0}/{1} to topic {2}, cv={3}",
    //         //     rawMessage.MessageId,
    //         //     this.topicName,
    //         //     this.operationContextProvider.Context?.CorrelationVector.Value);
    //
    //         await this._sender.SendMessageAsync(rawMessage, cancellationToken);
    //
    //         stopwatch.Stop();
    //         // this.telemetryLogger.Log(new MessagePostMetric(this.topicName, message, stopwatch.Elapsed));
    //     }
    //     catch (Exception ex)
    //     {
    //         // this.logger.LogError(
    //         //     "Posting ServiceBus message {0}/{1} to topic {2} failed, cv={3}\r\n{4}",
    //         //     message.GetType().Name,
    //         //     rawMessage.MessageId,
    //         //     this.topicName,
    //         //     this.operationContextProvider.Context?.CorrelationVector.Value,
    //         //     ex);
    //
    //         stopwatch.Stop();
    //         // this.telemetryLogger.Log(new MessagePostMetric(this.topicName, message, stopwatch.Elapsed, ex));
    //         throw;
    //     }
    // }
    //
    // private ServiceBusMessage BuildServiceBusMessage(
    //     DistopContext distopContext,
    //     CorrelationVector cv,
    //     bool expectResponse)
    // {
    //     var cloudEvent = new CloudEvent(
    //         CloudEventSource,
    //         nameof(DistopContext),
    //         distopContext);
    //     ServiceBusMessage message = new ServiceBusMessage(new BinaryData(cloudEvent));
    //
    //     // var args = JsonSerializer.Serialize(invocation.Arguments);
    //     // var genArgs = JsonSerializer.Serialize(invocation.GenericArguments);
    //     var serialized = JsonSerializer.Serialize(distopContext);
    //     // var serialized = JsonConvert.SerializeObject(distopContext);
    //     var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(serialized));
    //     serviceBusMessage.TimeToLive = TimeSpan.FromMinutes(10);
    //     serviceBusMessage.MessageId = Guid.NewGuid().ToString();
    //     serviceBusMessage.CorrelationId = cv.Value;
    //     serviceBusMessage.ApplicationProperties.Add("expect-reply", expectResponse);
    //     // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.SerializationMasterVersion, CurrentSerializationMasterVersion);
    //     // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.SerializationSubVersion, this.SerializationSubVersion);
    //     // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.MessageType, this.MessageType);
    //     // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.PostTime, DateTime.UtcNow);
    //     // serviceBusMessage.ApplicationProperties.Add(ApplicationProperties.TargetPool, currentPool);
    //
    //     return serviceBusMessage;
    // }
    //
    // private void Receive()
    // {
    //     // create the options to use for configuring the processor
    //     var options = new ServiceBusSessionProcessorOptions
    //     {
    //         // By default after the message handler returns, the processor will complete the message
    //         // If I want more fine-grained control over settlement, I can set this to false.
    //         // AutoCompleteMessages = false,
    //
    //         // I can also allow for processing multiple sessions
    //         MaxConcurrentSessions = 5,
    //
    //         // By default or when AutoCompleteMessages is set to true, the processor will complete the message after executing the message handler
    //         // Set AutoCompleteMessages to false to [settle messages](https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement#peeklock) on your own.
    //         // In both cases, if the message handler throws an exception without settling the message, the processor will abandon the message.
    //         MaxConcurrentCallsPerSession = 2,
    //
    //         // Processing can be optionally limited to a subset of session Ids.
    //         // SessionIds = { "my-session", "your-session" },
    //     };
    //
    //
    //     // create a session processor that we can use to process the messages
    //     await using ServiceBusSessionProcessor processor = _client.CreateSessionProcessor(topicName, options);
    //
    //     // configure the message and error handler to use
    //     processor.ProcessMessageAsync += MessageHandler;
    //     processor.ProcessErrorAsync += ErrorHandler;
    //
    //     var observable = Observable.Using(
    //         () => new JunkEnumerable(),
    //         junk => Observable.Generate(
    //             junk.GetEnumerator(),
    //             e => e.MoveNext(),
    //             e => e,
    //             e => e.Current,
    //             e => TimeSpan.FromMilliseconds(20)));
    //
    //     Observable.Create<ProcessSessionMessageEventArgs>(async (observer, token) =>
    //     {
    //         // CreateProcessor(topicName, subscriptionName
    //         await using ServiceBusSessionProcessor processor = _client.CreateSessionProcessor(topicName, options);
    //
    //         // configure the message and error handler to use
    //         processor.ProcessMessageAsync += args =>
    //         {
    //             if (args.CancellationToken.IsCancellationRequested)
    //             {
    //                 observer.OnError();
    //             }
    //             observer.OnNext(args.Message);
    //             return Task.CompletedTask;
    //
    //             // // Dead Letter immediately in case of permanent failure
    //             // catch (DistopPermanentException ex)
    //             // {
    //             //     await args.DeadLetterMessageAsync(args.Message, args.CancellationToken);
    //             // }
    //
    //         };
    //         processor.ProcessErrorAsync += ErrorHandler;
    //
    //
    //         observer.OnNext();
    //         return Disposable.Empty;
    //     })
    //
    //     Observable.FromEvent<ProcessSessionMessageEventArgs>(
    //         h => processor.ProcessMessageAsync += h,
    //         h => processor.ProcessMessageAsync -= h);
    //
    //     async Task MessageHandler(ProcessSessionMessageEventArgs args)
    //     {
    //         var body = args.Message.Body.ToString();
    //
    //         // we can evaluate application logic and use that to determine how to settle the message.
    //         await args.CompleteMessageAsync(args.Message);
    //
    //         // we can also set arbitrary session state using this receiver
    //         // the state is specific to the session, and not any particular message
    //         await args.SetSessionStateAsync(new BinaryData("some state"));
    //     }
    //
    //     Task ErrorHandler(ProcessErrorEventArgs args)
    //     {
    //         this.logger.LogError(
    //             args.Exception,
    //             "Error on processing ServiceBus message on topic {0}\r\nError Source={1}\n\nNamespace={2}\r\nEntityPath={3}\r\n{5}",
    //             this.topicName,
    //             args.ErrorSource,
    //             args.FullyQualifiedNamespace,
    //             args.EntityPath,
    //             args.Exception);
    //         return Task.CompletedTask;
    //         // the error source tells me at what point in the processing an error occurred
    //         Console.WriteLine(args.ErrorSource);
    //         // the fully qualified namespace is available
    //         Console.WriteLine(args.FullyQualifiedNamespace);
    //         // as well as the entity path
    //         Console.WriteLine(args.EntityPath);
    //         Console.WriteLine(args.Exception.ToString());
    //         return Task.CompletedTask;
    //     }
    //
    //     // start processing
    //     await processor.StartProcessingAsync();
    // }
}