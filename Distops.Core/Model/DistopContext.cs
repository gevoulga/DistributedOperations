using System.ComponentModel.DataAnnotations;

namespace Distops.Core.Model;

public record DistopContext
{
    public (SerializableType, object)[]? Arguments { get; set; }
    public GenericSerializableType[]? ArgumentTypes { get; set; }
    public SerializableType[]? GenericArguments { get; set; }
    public SerializableType MethodDeclaringObject { get; set; }
    [Required]
    public string MethodName { get; set; }
    public SerializableType? MethodReturnType { get; set; }

    public override string ToString() =>
        $"{MethodReturnType} " +
        // $"{MethodDeclaringObject}.{MethodName} " +
        $"{MethodName} " +
        $"({string.Join(", ", Arguments?.Select(tuple => tuple.Item1) ?? Enumerable.Empty<SerializableType>())})";
}