namespace Distops.Core.Model;

public record DistopContext
{
    public (SerializableType, object)[]? Arguments { get; set; }
    public GenericSerializableType[]? ArgumentTypes { get; set; }
    public SerializableType[]? GenericArguments { get; set; }
    public SerializableType MethodDeclaringObject { get; set; }
    public string MethodName { get; set; }
    public SerializableType? MethodReturnType { get; set; }
}