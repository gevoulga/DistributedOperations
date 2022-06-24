namespace Distops.Core.Model;

public readonly record struct SerializableType //: IYamlConvertible
{
    // private Type type;
    public string TypeName { get; init; }

    public static implicit operator Type(SerializableType value)
    {
        return Type.GetType(value.TypeName) ?? throw new InvalidOperationException($"Type {value.TypeName} not found");
    }

    public static implicit operator SerializableType(Type? value)
    {
        return new SerializableType { TypeName = value?.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(value)) };
    }

    public override string ToString()
    {
        return TypeName;
    }

    // void IYamlConvertible.Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
    // {
    //     var typeName = (string)nestedObjectDeserializer(typeof(string));
    //     type = typeName != null ? Type.GetType(typeName) : null;
    // }
    //
    // void IYamlConvertible.Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
    // {
    //     nestedObjectSerializer(type != null ? type.AssemblyQualifiedName : null);
    // }
    //
    // public static implicit operator Type(SerializableType value)
    // {
    //     return value.type;
    // }
    //
    // public static implicit operator SerializableType(Type value)
    // {
    //     return new SerializableType { type = value };
    // }
}