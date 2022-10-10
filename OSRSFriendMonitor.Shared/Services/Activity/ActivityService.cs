using OSRSFriendMonitor.Shared.Services.Account;
using OSRSFriendMonitor.Shared.Services.Database;
using OSRSFriendMonitor.Shared.Services.Database.Models;

namespace OSRSFriendMonitor.Shared.Services.Activity;

public interface IActivityService {
    Task ProcessActivityUpdateAsync(ActivityUpdate update);
}

public class ActivityService: IActivityService {
    private readonly IDatabaseService _databaseService;
    private readonly ILocationCache _locationCache;
    private readonly IAccountService _accountService;

    public ActivityService(IDatabaseService databaseService, ILocationCache locationCache, IAccountService accountService) {
        _databaseService = databaseService;
        _locationCache = locationCache;
        _accountService = accountService;
    }

    public async Task ProcessActivityUpdateAsync(ActivityUpdate update) {
        ActivityUpdate insertedUpdate = await _databaseService.InsertActivityUpdateAsync(update);

        if (insertedUpdate is LocationUpdate locationUpdate) 
        {
            _locationCache.AddLocationUpdate(
                new CachedLocationUpdateStruct(
                    X: locationUpdate.X, 
                    Y: locationUpdate.Y, 
                    Plane: locationUpdate.Plane,
                    RunescapeAccountIdentifier: locationUpdate.AccountIdentifier
                )
            );
        }
    }
}
