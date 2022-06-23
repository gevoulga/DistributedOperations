using System.Runtime.Serialization;
using Azure.Messaging.ServiceBus;

namespace Distops.Core.Model;

public class DistopTransientException : Exception
{
    public DistopTransientException()
    {
    }

    protected DistopTransientException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public DistopTransientException(string? message) : base(message)
    {
    }

    public DistopTransientException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}