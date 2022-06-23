namespace Distops.Core.Test.Samples;

public interface IFireAndForgetDistop
{
    void SyncFireAndForget();

    Task FireAndForget();
}