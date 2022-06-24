using Distops.Core.Model;
using Distops.Core.Services;

namespace Distops.InProcess.Services;

public class InProcessDistopClient : IDistopClient
{
    private readonly IDistopExecutor _distopExecutor;

    public InProcessDistopClient(IDistopExecutor distopExecutor)
    {
        _distopExecutor = distopExecutor;
    }

    public async Task<object?> Call(DistopContext distopContext, CancellationToken? cancellationToken = default)
    {
        var token = cancellationToken ?? CancellationToken.None;
        return await RunDistop(distopContext);
    }

    public Task FireAndForget(DistopContext distopContext, CancellationToken? cancellationToken = default)
    {
        var token = cancellationToken ?? CancellationToken.None;
        Task.Factory.StartNew(async () => await RunDistop(distopContext), token);
        return Task.CompletedTask;
    }

    private async Task<object?> RunDistop(DistopContext distopContext)
    {
        var result = await _distopExecutor.ExecuteDistop(distopContext);
        if (result.ExtractError(out var ok, out var ex))
        {
            throw ex;
        }

        return ok;
    }
}