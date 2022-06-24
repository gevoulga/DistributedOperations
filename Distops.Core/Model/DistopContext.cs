using System.ComponentModel.DataAnnotations;

namespace Distops.Core.Model;

public record DistopContext
{
    public object?[]? Arguments { get; init; }
    public List<GenericSerializableType> ArgumentTypes { get; init; }
    public List<SerializableType> GenericArguments { get; init; }
    public SerializableType MethodDeclaringObject { get; init; }
    [Required]
    public string MethodName { get; init; }
    public SerializableType MethodReturnType { get; init; }

    public override string ToString() =>
        $"{MethodReturnType} " +
        // $"{MethodDeclaringObject}.{MethodName} " +
        $"{MethodName} " +
        $"({string.Join(", ", ArgumentTypes)})";
}