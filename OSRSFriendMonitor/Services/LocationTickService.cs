using System.Diagnostics;

namespace OSRSFriendMonitor.Services;
public class LocationTickService : BackgroundService
{
    private readonly ILogger<LocationTickService> _logger;
    private readonly ILocalActivityBroadcaster _broadcaster;

    public LocationTickService(ILocalActivityBroadcaster broadcaster, ILogger<LocationTickService> logger)
    {
        _broadcaster = broadcaster;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (!stoppingToken.IsCancellationRequested &&
            await timer.WaitForNextTickAsync(stoppingToken))
        {
            _logger.LogInformation("running tick");
            Stopwatch watch = new();
            watch.Start();
            await _broadcaster.BroadcastLocationUpdatesToConnectedClientsAsync();
            watch.Stop();

            _logger.LogInformation("Location update broadcast took {duration} seconds", watch.ElapsedMilliseconds * 1000);
        }

    }
}