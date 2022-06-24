using Azure.Messaging;
using Azure.Messaging.ServiceBus;

namespace Distops.ServiceBus.Utils;

public static class CloudEventUtils
{
    private const string CloudEventSource = "/cloudevents/distops/servicebus";

    public static BinaryData ToCloudEvent<T>(this T arg)
    {
        return new BinaryData(new CloudEvent(
            CloudEventSource,
            typeof(T).Name,
            arg));
    }

    public static async Task<T> Parse<T>(this ProcessSessionMessageEventArgs args)
    {
        CloudEvent receivedCloudEvent = CloudEvent.Parse(args.Message.Body) ?? throw new ArgumentNullException();
        if (receivedCloudEvent.Type != typeof(T).Name)
        {
            await args.AbandonMessageAsync(args.Message, null, args.CancellationToken);
            throw new ArgumentException($"Unexpected type {receivedCloudEvent.Type} in response");
        }

        if (receivedCloudEvent.Data is null) throw new ArgumentNullException();
        return receivedCloudEvent.Data.ToObjectFromJson<T>();
    }

    public static async Task<T> Parse<T>(this ServiceBusReceivedMessage message,
        ServiceBusSessionReceiver acceptSessionAsync, CancellationToken cancellationToken)
    {
        CloudEvent receivedCloudEvent = CloudEvent.Parse(message.Body) ?? throw new ArgumentNullException();
        if (receivedCloudEvent.Type != typeof(T).Name)
        {
            await acceptSessionAsync.AbandonMessageAsync(message, null, cancellationToken);
            throw new ArgumentException($"Unexpected type {receivedCloudEvent.Type} in response");
        }

        if (receivedCloudEvent.Data is null) throw new ArgumentNullException();
        return receivedCloudEvent.Data.ToObjectFromJson<T>();
    }
}