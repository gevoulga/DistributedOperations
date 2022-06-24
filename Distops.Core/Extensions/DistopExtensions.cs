using Castle.DynamicProxy;
using Distops.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Distops.Core.Extensions;

public static class DistopExtensions
{
    public static IServiceCollection AddDistopExecutor(this IServiceCollection services)
    {
        services.TryAddSingleton<IDistopExecutor>(sp => new DistopExecutor(sp));
        return services;
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