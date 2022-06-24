using Microsoft.Extensions.Logging;

namespace Distops.Core.Test.Samples;

public class SyncDistop : ISyncDistop
{
    private readonly ILogger<SyncDistop> _logger;

    public SyncDistop(ILogger<SyncDistop> logger)
    {
        _logger = logger;
    }

    public long SyncCallReturns()
    {
        _logger.LogInformation("SyncCallReturns started");
        var ret = 111;
        _logger.LogInformation("SyncCallReturns finished");
        return ret;
    }

    public void SyncFireAndForget()
    {
        _logger.LogInformation("SyncFireAndForget started");
        Task.Delay(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        _logger.LogInformation("SyncFireAndForget finished");
    }
}