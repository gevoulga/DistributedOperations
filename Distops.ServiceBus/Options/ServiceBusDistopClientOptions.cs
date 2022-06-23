using System.ComponentModel.DataAnnotations;
using Azure.Messaging.ServiceBus;

namespace Distops.ServiceBus.Options;

public class ServiceBusDistopClientOptions
{
    public const string Section = "ServiceBusDistopClient";

    [Required]
    public string ConnectionString { get; set; }
    [Required]
    public string TopicName { get; set; }
    [Required]
    public string SubscriptionName { get; set; }
    [Required]
    public string InstanceName { get; set; }
    public ServiceBusClientOptions ServiceBusClientOptions  { get; set; }
    public ServiceBusSessionReceiverOptions ServiceBusSessionReceiverOptions { get; set; }
    public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(30);
}