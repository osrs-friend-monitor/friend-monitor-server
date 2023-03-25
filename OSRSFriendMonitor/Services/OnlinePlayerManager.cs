using OSRSFriendMonitor.Shared.Services.Cache;

namespace OSRSFriendMonitor.Services;

public class OnlinePlayerManager
{
    private string _instanceId;
    private long[] _onlinePlayers;
    private IRemoteCache _cache;
    public OnlinePlayerManager(string instanceId)
    {
        _instanceId = instanceId;
        _onlinePlayers = Array.Empty<long>();
    }
    public long[] GetOnlinePlayers()
    {
        return _onlinePlayers;
    }
    public async Task UpdateOnlinePlayers()
    {

    }
}
