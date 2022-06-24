using System.Diagnostics;
using System.Reflection;
using Distops.Core.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Distops.Core.Services;

public class DistopExecutor : IDistopExecutor
{
    private readonly ILogger<DistopExecutor> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DistopExecutor(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<DistopExecutor>>();
        _serviceProvider = serviceProvider;
    }

    // public static TInterface CreateProxy<TInterface>(ILogger<Interceptor> logger)
    //     where TInterface : class
    // {
    //
    //     // var proxy = new ProxyGenerator()
    //     //     .CreateInterfaceProxyWithTarget(typeof(IDistop), distopImpl, interceptor) as IDistop;
    //     var interceptor = new Interceptor(logger);
    //     return new ProxyGenerator()
    //         .CreateInterfaceProxyWithoutTarget<TInterface>(interceptor);
    // }

    public async Task<Result<object?, Exception>> ExecuteDistop(DistopContext distopContext)
    {
        var watch = Stopwatch.StartNew();
        _logger.LogInformation("Distop started: '{}'", distopContext);

        var target = ResolveTarget(distopContext);
        var methodInfo = ResolveMethod(distopContext, target);
        Type methodReturnType = distopContext.MethodReturnType;
        var parameters = distopContext.Arguments
            .Select(tuple => tuple.Item2)
            .ToArray();

        try
        {
            var returnedValue = methodInfo.Invoke(target, parameters);
            var ret = await IsValidTypeAndReturn(methodReturnType, returnedValue);
            return new Result<object?, Exception>.Success(ret);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex,"Distop: '{}' threw exception", distopContext);
            return ex;
        }
        finally
        {
            watch.Stop();
            _logger.LogInformation("Distop finished: '{}', elapsed '{}'", distopContext, watch.Elapsed);
            // TODO add telemetry metrics
        }
    }

    private object ResolveTarget(DistopContext distopContext)
    {
        Type targetType = distopContext.MethodDeclaringObject;
        var target = _serviceProvider.GetRequiredService(targetType);
        return target;
    }

    private MethodInfo ResolveMethod(DistopContext distopContext, object target)
    {
        var genericParameterCount = distopContext.GenericArguments?.Length ?? 0;
        var parameterTypes = distopContext.ArgumentTypes
            .Select((t, i) => t.GetType(i))
            .ToArray();
        var methodInfo = target.GetType().GetMethod(distopContext.MethodName, genericParameterCount, parameterTypes)
                         ?? throw new InvalidOperationException($"Method not found for {distopContext}");
        return methodInfo;
    }

    private async Task<object?> IsValidTypeAndReturn(Type? methodReturnType, object? returnedValue)
    {
        var type = returnedValue?.GetType();
        bool IsSync() => methodReturnType?.IsAssignableFrom(type) ?? false;
        bool IsVoid() => methodReturnType?.IsAssignableFrom(typeof(void)) ?? false;
        bool IsTask() => methodReturnType?.IsAssignableFrom(typeof(Task)) ?? false;
        bool IsGenericTask() => (methodReturnType?.IsGenericType ?? false) && (methodReturnType?.GetGenericTypeDefinition().IsAssignableFrom(typeof(Task<>)) ?? false);

        if (IsTask())
        {
            var task = (Task) returnedValue;
            await task.ConfigureAwait(false);
            return null;
        }
        if (IsGenericTask())
        {
            return returnedValue;
            // Task task = (Task) returnedValue;
            // // Make sure it runs to completion
            // await task.ConfigureAwait(false);
            // // Harvest the result
            // return (object)((dynamic)task).Result;
        }
        else if (IsVoid() || IsSync())
        {
            return returnedValue;
        }

        throw new InvalidCastException($"Cannot cast {returnedValue} to {methodReturnType}");
    }

    // private Task<object?> TaskReturnValue(Type? methodReturnType, object? returnedValue)
    // {
    //     bool isBar = foo.GetType().GetInterfaces().Any(x =>
    //         x.IsGenericType &&
    //         x.GetGenericTypeDefinition() == typeof(Tas<>));
    //     bool IsTask() => methodReturnType?.IsAssignableFrom(typeof(Task<>)) ?? false;
    // }
}