using System.Diagnostics;

namespace OSRSFriendMonitor.Services;

public interface ITickAccessor
{
    ulong GetTick();
}

public interface ITickIncrementer: ITickAccessor
{
    void Increment();
}

public class TickService: ITickIncrementer
{
    private ulong _tick = 0;

    ulong ITickAccessor.GetTick()
    {
        return _tick;
        throw new NotImplementedException();
    }

    void ITickIncrementer.Increment()
    {
        Interlocked.Increment(ref _tick);
    }
}
public class LocationTickService : BackgroundService
{
    private readonly ILogger<LocationTickService> _logger;
    private readonly ILocalActivityBroadcaster _broadcaster;
    private readonly ITickIncrementer _tickIncrementer;
    public LocationTickService(ILocalActivityBroadcaster broadcaster, ILogger<LocationTickService> logger, ITickIncrementer tickIncrementer)
    {
        _broadcaster = broadcaster;
        _logger = logger;
        _tickIncrementer = tickIncrementer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(0.6));

        while (!stoppingToken.IsCancellationRequested &&
            await timer.WaitForNextTickAsync(stoppingToken))
        {
            //_logger.LogInformation("running tick");
            Stopwatch watch = new();
            watch.Start();
            await _broadcaster.BroadcastLocationUpdatesToConnectedClientsAsync(_tickIncrementer.GetTick());
            watch.Stop();

            //_logger.LogInformation("Location update broadcast took {duration} seconds", watch.Elapsed.TotalSeconds);

            _tickIncrementer.Increment();
        }

    }
}