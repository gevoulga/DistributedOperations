using System.Diagnostics;
using Distops.Core.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Distops.Core.Services;

public class DistopInterceptor : BaseDistopInterceptor
{
    private readonly ILogger<DistopInterceptor> _logger;
    private readonly IDistopClient _distopClient;

    internal DistopInterceptor(IServiceProvider sp)
        : base(sp.GetRequiredService<ILogger<DistopInterceptor>>())
    {
        _distopClient = sp.GetRequiredService<IDistopClient>();
        _logger = sp.GetRequiredService<ILogger<DistopInterceptor>>();
    }

    protected override object? ExecuteRemote(DistopContext distopContext, Type methodReturnType)
    {
        var watch = Stopwatch.StartNew();
        _logger.LogInformation("Execute remote distop started: '{}'", distopContext);

        // Check for the flag fire and forget
        bool IsTask() => methodReturnType.IsAssignableFrom(typeof(Task));
        bool IsGenericTask() => (methodReturnType?.IsGenericType ?? false) && (methodReturnType?.GetGenericTypeDefinition().IsAssignableFrom(typeof(Task<>)) ?? false);

        try
        {
            // TODO passing down a cancellation token
            var returnedValue = _distopClient.Call(distopContext);

            // Replace the return value so that it only completes when the post-interception code is complete.
            // invocation.ReturnValue = InterceptAsync(returnedValue);

            if (IsTask())
            {
                return returnedValue;
            }
            else if (IsGenericTask())
            {
                return returnedValue.Result;
            }
            else
            {
                return returnedValue.Result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute remote distop: {} threw exception", distopContext);
            throw;
        }
        finally
        {
            watch.Stop();
            _logger.LogInformation("Execute remote distop finished: '{}', elapsed '{}'", distopContext, watch.Elapsed);
            // TODO add telemetry metrics
        }
    }
}