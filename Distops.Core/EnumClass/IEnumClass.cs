namespace Distops.Core.EnumClass;

public interface IEnumClass<T>
{
}

public interface IEnumClass<T, out T1, out T2> : IEnumClass<T>
    where T1 : T
    where T2 : T
{
}

public interface IEnumClass<T, out T1, out T2, out T3> : IEnumClass<T>
    where T1 : T
    where T2 : T
    where T3 : T
{
}

public interface IEnumClass<T, out T1, out T2, out T3, out T4> : IEnumClass<T>
    where T1 : T
    where T2 : T
    where T3 : T
    where T4 : T
{

}

public interface IEnumClass<T, out T1, out T2, out T3, out T4, out T5> : IEnumClass<T>
    where T1 : T
    where T2 : T
    where T3 : T
    where T4 : T
    where T5 : T
{
}