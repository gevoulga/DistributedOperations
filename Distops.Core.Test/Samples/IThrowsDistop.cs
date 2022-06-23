namespace Distops.Core.Test.Samples;

public interface IThrowsDistop
{

    void ThrowsSync();
    Task ThrowsAsync();
    Task<long> ThrowsAsyncLong();
}