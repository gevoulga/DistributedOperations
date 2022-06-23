namespace Distops.Core.Test.Samples;

public class ThrowsDistop : IThrowsDistop
{
    public void ThrowsSync()
    {
        throw new InvalidOperationException("Expected");
    }

    public async Task ThrowsAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        throw new NotImplementedException();
    }

    public async Task<long> ThrowsAsyncLong()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        throw new NotImplementedException();
    }
}