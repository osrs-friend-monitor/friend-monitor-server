using OSRSFriendMonitor.Shared.Services.Account;
using OSRSFriendMonitor.Shared.Services.Activity;
using OSRSFriendMonitor.Shared.Services.Database.Models;

namespace OSRSFriendMonitor.Services;

public interface IActivityProcessor
{
    public Task ProcessActivityAsync(ActivityUpdate update, string userId);
}

public class ActivityProcessor: IActivityProcessor
{
    private readonly IActivityStorageService _storageService;
    private readonly IAccountStorageService _accountStorageService;
    private readonly ILocalActivityBroadcaster _localBroadcaster;
    private readonly IRemoteActivityBroadcaster _remoteBroadcaster;
    private readonly ILogger<ActivityProcessor> _logger;

    public ActivityProcessor(
        IActivityStorageService storageService, 
        ILocalActivityBroadcaster localBroadcaster,
        IRemoteActivityBroadcaster remoteBroadcaster,
        IAccountStorageService accountStorageService,
        ILogger<ActivityProcessor> logger
    )
    {
        _storageService = storageService;
        _localBroadcaster = localBroadcaster;
        _remoteBroadcaster = remoteBroadcaster;
        _accountStorageService = accountStorageService;
        _logger = logger;
    }

    public async Task ProcessActivityAsync(ActivityUpdate update, string userId)
    {
        RunescapeAccount? account = await _accountStorageService.GetRunescapeAccountAsync(update.AccountHash);

        if (account == null || account.UserId != userId)
        {
            _logger.LogWarning("user ID {userId} does not match account userID {accountUserId}", userId, account?.UserId);
            return;
        }

        await _storageService.StoreActivityUpdateAsync(update);

        bool handledLocally = await _localBroadcaster.BroadcastActivityAsync(update);
        
        if (handledLocally)
        {
            return;
        }

        await _remoteBroadcaster.BroadcastActivityAsync(update);
    }
}
