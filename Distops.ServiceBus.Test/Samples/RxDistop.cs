using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Distops.Core.Test.Samples;
using Microsoft.Extensions.Logging;

namespace Distops.ServiceBus.Test.Samples;

public class RxDistop : IAsyncDistop
{
    private readonly ILogger<RxDistop> _logger;
    private readonly IObservable<long> _tickStream;

    public RxDistop(ILogger<RxDistop> logger)
    {
        this._logger = logger;
        _tickStream = Observable.Interval(TimeSpan.FromSeconds(1))
            .Publish()
            .RefCount();
    }

    public long SyncCallReturns()
    {
        return 111;
    }

    public void SyncFireAndForget()
    {
        Task.Delay(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
    }

    public async Task FireAndForget()
    {
        await Task.Delay(TimeSpan.FromSeconds(10));
    }

    public virtual async Task DoSomething<T>(DistopDto distopDto, T t, CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        _logger.LogInformation($"DoSomething started with args: {nameof(distopDto)}: {distopDto}, {nameof(t)}: {t}");

        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

        watch.Stop();
        _logger.LogInformation($"DoSomething finished in {watch.Elapsed}");
    }

    public virtual async Task<long> JustALong()
    {
        await Task.Delay(TimeSpan.FromSeconds(10));
        return 3;
    }

    public virtual async Task<long> CurrentTick(DistopDto distopDto, CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        _logger.LogInformation($"CurrentTick started with args: {nameof(distopDto)}: {distopDto}");

        try
        {
            var tick = await _tickStream.Take(3).ToTask(cancellationToken);
            _logger.LogInformation($"CurrentTick: {tick}");
            return tick;
        }
        finally
        {
            watch.Stop();
            _logger.LogInformation($"CurrentTick finished in {watch.Elapsed}");
        }
    }

    public virtual IObservable<long> Ticks(DistopDto distopDto)
    {
        var watch = Stopwatch.StartNew();
        _logger.LogInformation($"Ticks started with args: {nameof(distopDto)}: {distopDto}");

        try
        {
            return _tickStream;
        }
        finally
        {
            watch.Stop();
            _logger.LogInformation($"Ticks finished in {watch.Elapsed}");
        }
    }
}