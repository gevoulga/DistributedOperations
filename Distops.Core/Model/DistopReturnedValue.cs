namespace Distops.Core.Model;

public record DistopReturnedValue
{
    // TODO returning exceptions
    public object? Value { get; init; }
}