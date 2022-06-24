using Distops.Core.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Distops.Core.Services;

public class DistopFireAndForgetInterceptor : BaseDistopInterceptor
{
    private readonly IDistopClient _distopClient;

    internal DistopFireAndForgetInterceptor(IServiceProvider sp)
        : base(sp.GetRequiredService<ILogger<DistopInterceptor>>())
    {
        _distopClient = sp.GetRequiredService<IDistopClient>();
    }

    protected override object? ExecuteRemote(DistopContext distopContext, Type methodReturnType)
    {
        bool IsTask() => methodReturnType.IsAssignableFrom(typeof(Task));
        // TODO passing down a cancellation token
        var task = _distopClient.FireAndForget(distopContext);
        return IsTask() ? task : null;
    }
}