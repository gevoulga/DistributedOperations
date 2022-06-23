namespace Distops.Core.Model;

public struct GenericSerializableType //: IYamlConvertible
{
    // private Type type;
    private string _typeName;

    public static implicit operator GenericSerializableType(Type? value)
    {
        return new GenericSerializableType { _typeName = value?.AssemblyQualifiedName ?? value?.Name ?? throw new ArgumentNullException(nameof(value)) };
    }

    public Type GetType(int position)
    {
        return Type.GetType(_typeName) ?? Type.MakeGenericMethodParameter(position);
    }

    public override string ToString()
    {
        return $"{nameof(_typeName)}: {_typeName}";
    }
}