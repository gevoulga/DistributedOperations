using Castle.DynamicProxy;

namespace Distops.Core.Services;

public static class DistopExtensions
{
    public static IServiceCollection AddDistopsService<T>(this IServiceCollection serviceCollection)
        where T : class, IDistopService
    {
        return serviceCollection
            .AddSingleton<IDistopExecutor>(sp => new DistopExecutor(sp))
            .AddSingleton<IDistopService, T>();
    }

    public static IServiceCollection AddDistopsService<T>(
        this IServiceCollection serviceCollection,
        Func<IServiceProvider, T> distopServiceProvider)
        where T : class, IDistopService
    {
        return serviceCollection
            .AddSingleton<IDistopExecutor>(sp => new DistopExecutor(sp))
            .AddSingleton<IDistopService, T>(distopServiceProvider);
    }

    public static TInterface GetDistop<TInterface>(this IServiceProvider sp)
        where TInterface : class
    {
        var interceptor = new DistopInterceptor(sp);
        return new ProxyGenerator()
            .CreateInterfaceProxyWithoutTarget<TInterface>(interceptor);
    }

    public static TInterface GetFireAndForgetDistop<TInterface>(this IServiceProvider sp)
        where TInterface : class
    {
        var interceptor = new DistopFireAndForgetInterceptor(sp);
        ValidateFireAndForget(typeof(TInterface));
        return new ProxyGenerator()
            .CreateInterfaceProxyWithoutTarget<TInterface>(interceptor);
    }

    private static void ValidateFireAndForget(Type interfaceType)
    {
        var methodInfos = interfaceType.GetMethods();
        foreach (var methodInfo in methodInfos)
        {
            // Make sure that the return type is Task or void?
            if (methodInfo.ReturnType.IsAssignableFrom(typeof(Task)))
            {
                continue;
            }
            else if (methodInfo.ReturnType.IsAssignableFrom(typeof(void)))
            {
                continue;
            }

            throw new InvalidOperationException(
                $"Cannot use fire and forget configured distop for method '{interfaceType}.{methodInfo.Name}' with return type '{methodInfo.ReturnType}'");
            // string res = (methodInfo.ReturnType) switch
            // {
            //
            //     (Task) => "1 and A",
            //     (2, "B") => "b",
            //     _ => "default"
            // };
        }
    }
}