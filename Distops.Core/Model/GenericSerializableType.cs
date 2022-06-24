namespace Distops.Core.Model;

public readonly record struct GenericSerializableType //: IYamlConvertible
{
    // private Type type;
    public string TypeName { get; init; }

    public static implicit operator GenericSerializableType(Type? value)
    {
        return new GenericSerializableType { TypeName = value?.AssemblyQualifiedName ?? value?.Name ?? throw new ArgumentNullException(nameof(value)) };
    }

    public Type GetType(int position)
    {
        return Type.GetType(TypeName) ?? Type.MakeGenericMethodParameter(position);
    }

    public override string ToString()
    {
        return $"{nameof(TypeName)}: {TypeName}";
    }
}