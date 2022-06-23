using System.Runtime.Serialization;

namespace Distops.Core.Model;

public class DistopPermanentException : Exception
{
    public DistopPermanentException()
    {
    }

    protected DistopPermanentException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public DistopPermanentException(string? message) : base(message)
    {
    }

    public DistopPermanentException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}