using System.Reflection;

namespace Distops.Core.EnumClass;

public static class EnumClassExtensions
{
    public static TResult Switch<TResult, T, T1, T2>(
        this IEnumClass<T,T1,T2> enumerableClass,
        Func<T1, TResult> onT1,
        Func<T2, TResult> onT2)
        where T1 : T
        where T2 : T
    {
        _ = onT1 ?? throw new ArgumentNullException(nameof(onT1));
        _ = onT2 ?? throw new ArgumentNullException(nameof(onT2));
        ValidateImplementations<T>(2);
        return enumerableClass switch
        {
            T1 t1 => onT1(t1),
            T2 t2 => onT2(t2),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static TResult Switch<TResult, T, T1, T2, T3>(
        this IEnumClass<T,T1,T2,T3> enumerableClass,
        Func<T1, TResult> onT1,
        Func<T2, TResult> onT2,
        Func<T3, TResult> onT3)
        where T1 : T
        where T2 : T
        where T3 : T
    {
        _ = onT1 ?? throw new ArgumentNullException(nameof(onT1));
        _ = onT2 ?? throw new ArgumentNullException(nameof(onT2));
        _ = onT3 ?? throw new ArgumentNullException(nameof(onT3));
        ValidateImplementations<T>(3);
        return enumerableClass switch
        {
            T1 t1 => onT1(t1),
            T2 t2 => onT2(t2),
            T3 t3 => onT3(t3),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static TResult Switch<TResult, T, T1, T2, T3, T4>(
        this IEnumClass<T,T1,T2,T3,T4> enumerableClass,
        Func<T1, TResult> onT1,
        Func<T2, TResult> onT2,
        Func<T3, TResult> onT3,
        Func<T4, TResult> onT4)
        where T1 : T
        where T2 : T
        where T3 : T
        where T4 : T
    {
        _ = onT1 ?? throw new ArgumentNullException(nameof(onT1));
        _ = onT2 ?? throw new ArgumentNullException(nameof(onT2));
        _ = onT3 ?? throw new ArgumentNullException(nameof(onT3));
        _ = onT4 ?? throw new ArgumentNullException(nameof(onT4));
        ValidateImplementations<T>(4);
        return enumerableClass switch
        {
            T1 t1 => onT1(t1),
            T2 t2 => onT2(t2),
            T3 t3 => onT3(t3),
            T4 t4 => onT4(t4),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static TResult Switch<TResult, T, T1, T2, T3, T4, T5>(
        this IEnumClass<T,T1,T2,T3,T4, T5> enumerableClass,
        Func<T1, TResult> onT1,
        Func<T2, TResult> onT2,
        Func<T3, TResult> onT3,
        Func<T4, TResult> onT4,
        Func<T5, TResult> onT5)
        where T1 : T
        where T2 : T
        where T3 : T
        where T4 : T
        where T5 : T
    {
        _ = onT1 ?? throw new ArgumentNullException(nameof(onT1));
        _ = onT2 ?? throw new ArgumentNullException(nameof(onT2));
        _ = onT3 ?? throw new ArgumentNullException(nameof(onT3));
        _ = onT4 ?? throw new ArgumentNullException(nameof(onT4));
        _ = onT5 ?? throw new ArgumentNullException(nameof(onT5));
        ValidateImplementations<T>(5);
        return enumerableClass switch
        {
            T1 t1 => onT1(t1),
            T2 t2 => onT2(t2),
            T3 t3 => onT3(t3),
            T4 t4 => onT4(t4),
            T5 t5 => onT5(t5),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static void ValidateImplementations<T>(int expectedNumberOfImplementations)
    {
        var implementations = Assembly.GetAssembly(typeof(T))?
            .GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T)))
            .ToArray() ?? Array.Empty<Type>();
        if (implementations.Length != expectedNumberOfImplementations)
        {
            throw new ArgumentOutOfRangeException(nameof(T), $"Expected {typeof(T)} to be implemented by {expectedNumberOfImplementations} direct classes/interfaces, but is implemented by {string.Join(", ", implementations.Select(type => type.Name))}");
        }
    }
}