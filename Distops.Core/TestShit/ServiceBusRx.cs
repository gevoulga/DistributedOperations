using System.Reactive.Disposables;
using System.Reactive.Linq;
using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Distops.Core.Model;

namespace Distops.Core.TestShit;

public class ServiceBusRx
{
    private readonly string topicName;
    private readonly string subscriptionName;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private ILogger<ServiceBusDistopService> _logger;

    private void RxStream()
    {
        // create the options to use for configuring the processor
        var options = new ServiceBusProcessorOptions
        {
            // By default or when AutoCompleteMessages is set to true, the processor will complete the message after executing the message handler
            // Set AutoCompleteMessages to false to [settle messages](https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement#peeklock) on your own.
            // In both cases, if the message handler throws an exception without settling the message, the processor will abandon the message.
            // AutoCompleteMessages = false,

            // I can also allow for multi-threading
            MaxConcurrentCalls = 2
        };

        var serviceBusRxStream = Observable.Create<ProcessMessageEventArgs>(async (observer, cancellationToken) =>
        {
            // The message and error handlers
            Task MessageHandler(ProcessMessageEventArgs args)
            {
                // We need to cancel the processor
                if (cancellationToken.IsCancellationRequested) return Task.CompletedTask;

                try
                {
                    observer.OnNext(args);
                }
                catch (Exception ex)
                {
                    // Is this try catch needed?
                    observer.OnError(ex);
                }

                return Task.CompletedTask;
            }
            Task ErrorHandler(ProcessErrorEventArgs args)
            {
                _logger.LogError(args.Exception, "Error on processing ServiceBus message on topic {0}\r\nError Source={1}\n\nNamespace={2}\r\nEntityPath={3}\r\n{5}", this.topicName, args.ErrorSource, args.FullyQualifiedNamespace, args.EntityPath, args.Exception);
                return Task.CompletedTask;
            }

            // Subscribe the handlers and start the processor
            // await using ServiceBusSessionProcessor processor = _client.CreateSessionProcessor(topicName, subscriptionName, options);
            var processor = _client.CreateProcessor(topicName, subscriptionName, options);
            processor.ProcessMessageAsync += MessageHandler;
            processor.ProcessErrorAsync += ErrorHandler;
            await processor.StartProcessingAsync(cancellationToken);

            // On dispose of the subscription stop the processor and remove the handlers
            return Disposable.Create(() =>
            {
                processor.StopProcessingAsync(CancellationToken.None).GetAwaiter().GetResult();
                // var task = Task.Run(() => processor.StopProcessingAsync(CancellationToken.None), CancellationToken.None);
                // task.Wait(CancellationToken.None);
                processor.ProcessMessageAsync -= MessageHandler;
                processor.ProcessErrorAsync -= ErrorHandler;
            });
        });

        serviceBusRxStream
            .SubscribeAsync(async args =>
            {
                // Complete the message
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            }, async exception =>
            {
                if (exception is DistopPermanentException distopPermanentException)
                {
                    //await distopPermanentException.DisposeAsync();
                }
            })
            .Subscribe(CancellationToken.None);


        // Renew messages as long as the stream is alive
        // _args.RenewMessageLockAsync(_args.Message, _args.CancellationToken)
    }

    private class RxValue : IAsyncDisposable
    {
        private readonly ProcessMessageEventArgs _args;


        // To be executed when the receiver is finished processing the service bus message
        public async ValueTask DisposeAsync()
        {
            await _args.CompleteMessageAsync(_args.Message, _args.CancellationToken);
        }
    }

    private DistopContext ToDistop(ServiceBusReceivedMessage receivedMessage)
    {
        // Deserialize the message body into a CloudEvent
        CloudEvent receivedCloudEvent = CloudEvent.Parse(receivedMessage.Body);
        return receivedCloudEvent.Data.ToObjectFromJson<DistopContext>();
    }

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
    //
    //     // var subject = new Subject<ServiceBusReceivedMessage>();
    //     // subject.Get
    //     // var asyncSubject = new AsyncSubject<ServiceBusReceivedMessage>();
    //     // asyncSubject.
    //
    //
    //
    //
    //     // create a session processor that we can use to process the messages
    //     await using ServiceBusSessionProcessor processor = _client.CreateSessionProcessor(topicName, subscriptionName, options);
    //
    //
    //
    //     var obs = Observable.Create<TMessage>(observer =>
    //     {
    //         var tokenSource = new CancellationTokenSource();
    //         var token = tokenSource.Token;
    //         var subscribedEvent = new ManualResetEventSlim(false);
    //
    //         var task = Task.Run(() => Subscriber(token, observer, subscribedEvent));
    //
    //         subscribedEvent.Wait();
    //
    //         return Disposable.Create(() =>
    //         {
    //             tokenSource.Cancel();
    //             task.Wait();
    //         });
    //     });
    //
    //
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
    //         await using ServiceBusSessionProcessor processor = _client.CreateSessionProcessor(topicName, options);
    //
    //         // configure the message and error handler to use
    //         processor.ProcessMessageAsync += args =>
    //         {
    //             if (args.CancellationToken.IsCancellationRequested)
    //             {
    //                 observer.OnError();
    //             }
    //
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
    // }
    //
    // private async Task Subscriber(CancellationToken token, IObserver<ServiceBusReceivedMessage> observer, ManualResetEventSlim subscribedEvent)
    // {
    //     try
    //     {
    //         using (var socket = new SubscriberSocket())
    //         {
    //             socket.Start(_address, _socketType);
    //             socket.Subscribe(_topic);
    //             subscribedEvent.Set();
    //
    //             while (!token.IsCancellationRequested)
    //             {
    //                 ReceiveMessage(observer, socket);
    //             }
    //
    //             socket.Unsubscribe(_topic);
    //             socket.Stop(_address, _socketType);
    //         }
    //     }
    //     finally
    //     {
    //         if (!subscribedEvent.IsSet)
    //         {
    //             subscribedEvent.Set();
    //         }
    //     }
    // }
    //
    // private void ReceiveMessage(IObserver<TMessage> observer, SubscriberSocket socket)
    // {
    //     try
    //     {
    //         string topic;
    //         byte[] rawMessage;
    //         string typeName;
    //
    //         if (socket.TryReceive(_receiveTimeout, out topic, out typeName, out rawMessage) && MatchesTopic(topic))
    //         {
    //             var message = typeName == null
    //                 ? _deserializers.Values.Single().Deserialize(rawMessage)
    //                 : _deserializers[typeName].Deserialize(rawMessage);
    //
    //             observer.OnNext(message);
    //         }
    //     }
    //     catch (Exception exception)
    //     {
    //         observer.OnError(exception);
    //     }
    // }
    //
    // private bool MatchesTopic(string topic)
    // {
    //     return topic == _topic;
    // }
}