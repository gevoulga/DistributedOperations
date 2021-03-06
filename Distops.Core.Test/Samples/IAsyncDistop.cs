namespace Distops.Core.Test.Samples;

public interface IAsyncDistop
{
    Task FireAndForget();
    Task DoSomething<T>(DistopDto distopDto, T t, CancellationToken cancellationToken);
    Task<long> JustALong();
    Task<long> CurrentTick(DistopDto distopDto, CancellationToken cancellationToken);
    IAsyncEnumerable<long> Ticks(DistopDto distopDto);
}