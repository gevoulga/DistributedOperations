namespace Distops.Core.Model;

public struct SerializableType //: IYamlConvertible
{
    // private Type type;
    private string _typeName;

    public static implicit operator Type(SerializableType value)
    {
        return Type.GetType(value._typeName) ?? throw new InvalidOperationException($"Type {value._typeName} not found");
    }

    public static implicit operator SerializableType(Type? value)
    {
        return new SerializableType { _typeName = value?.AssemblyQualifiedName ?? throw new ArgumentNullException(nameof(value)) };
    }

    public override string ToString()
    {
        return $"{nameof(_typeName)}: {_typeName}";
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