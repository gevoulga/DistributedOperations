using Microsoft.Extensions.Logging;

namespace Distops.Core.Test.Samples;

public class FireAndForgetDistop : IFireAndForgetDistop
{
    private readonly ILogger<FireAndForgetDistop> _logger;

    public FireAndForgetDistop(ILogger<FireAndForgetDistop> logger)
    {
        _logger = logger;
    }

    public void SyncFireAndForget()
    {
        _logger.LogInformation("SyncFireAndForget started");
        Task.Delay(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        _logger.LogInformation("SyncFireAndForget finished");
    }

    public async Task FireAndForget()
    {
        _logger.LogInformation("FireAndForget started");
        await Task.Delay(TimeSpan.FromSeconds(10));
        _logger.LogInformation("FireAndForget finished");
    }
}