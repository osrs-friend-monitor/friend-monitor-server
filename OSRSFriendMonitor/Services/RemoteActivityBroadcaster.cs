using OSRSFriendMonitor.Shared.Services.Database.Models;

namespace OSRSFriendMonitor.Services;

public interface IRemoteActivityBroadcaster
{
    public Task BroadcastActivityAsync(ActivityUpdate update);
}

public class RemoteActivityBroadcaster : IRemoteActivityBroadcaster
{
    Task IRemoteActivityBroadcaster.BroadcastActivityAsync(ActivityUpdate update)
    {
        throw new NotImplementedException();
    }
}
