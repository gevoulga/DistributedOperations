using System.Diagnostics;
using System.Reflection;
using Castle.DynamicProxy;

namespace Distops.Core.Model;

public abstract class BaseDistopInterceptor : IInterceptor
{
    private readonly ILogger _logger;

    protected BaseDistopInterceptor(ILogger logger)
    {
        _logger = logger;
    }

    protected abstract object? ExecuteRemote(DistopContext distopContext, Type methodReturnType);

    public void Intercept(IInvocation invocation)
    {
        var watch = Stopwatch.StartNew();
        _logger.LogInformation($"Starting distop '{invocation.Method.Name}' with args: '{invocation.Arguments}'" );
        try
        {
            var distopContext = ResolveDistopContext(invocation);

            invocation.ReturnValue = ExecuteRemote(distopContext, invocation.Method.ReturnType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Distop threw exception");
            throw;
        }
        finally
        {
            watch.Stop();
            _logger.LogInformation($"Finished distop '{invocation.Method.Name}', elapsed '{watch.Elapsed}'");
        }
    }

    // // This method will complete when PostInterceptAsync completes.
    // private async Task InterceptAsync(Task originalTask)
    // {
    //     // Asynchronously wait for the original task to complete
    //     await originalTask;
    //
    //     // Asynchronous post-execution
    //     // await PostInterceptAsync();
    // }

    private DistopContext ResolveDistopContext(IInvocation invocation)
    {
        var arguments = invocation.Arguments?
            .Select<object, (SerializableType type, object obj)>(obj => (obj.GetType(), obj))
            .ToArray();
        var genericArguments = invocation.GenericArguments?
            .Select<Type, SerializableType>(genericType => genericType).ToArray();
        var argumentTypes = invocation.Method.GetParameters()
            .Select<ParameterInfo, GenericSerializableType>(parameterInfo => parameterInfo.ParameterType)
            .ToArray();

        return new DistopContext()
        {
            Arguments = arguments,
            ArgumentTypes = argumentTypes,
            GenericArguments = genericArguments,
            MethodDeclaringObject = invocation.Method.DeclaringType,
            MethodName = invocation.Method.Name,
            MethodReturnType = invocation.Method.ReturnType,
        };
    }
}