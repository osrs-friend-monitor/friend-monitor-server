using OSRSFriendMonitor.Shared.Services.Activity;
using OSRSFriendMonitor.Shared.Services.Database.Models;

namespace OSRSFriendMonitor.Services;

public interface IActivityProcessor
{
    public Task ProcessActivityAsync(ActivityUpdate update);
}

public class ActivityProcessor: IActivityProcessor
{
    private readonly IActivityStorageService _storageService;
    private readonly ILocalActivityBroadcaster _localBroadcaster;
    private readonly IRemoteActivityBroadcaster _remoteBroadcaster;

    public ActivityProcessor(IActivityStorageService storageService, 
                             ILocalActivityBroadcaster localBroadcaster,
                             IRemoteActivityBroadcaster remoteBroadcaster)
    {
        _storageService = storageService;
        _localBroadcaster = localBroadcaster;
        _remoteBroadcaster = remoteBroadcaster;
    }

    public async Task ProcessActivityAsync(ActivityUpdate update)
    {
        await _storageService.StoreActivityUpdateAsync(update);

        bool handledLocally = await _localBroadcaster.BroadcastActivityAsync(update);
        
        if (handledLocally)
        {
            return;
        }

        await _remoteBroadcaster.BroadcastActivityAsync(update);

    }
}
