using System.Runtime.Serialization;

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