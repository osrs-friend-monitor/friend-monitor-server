using OSRSFriendMonitor.Shared.Services.Account;
using OSRSFriendMonitor.Shared.Services.Database;
using OSRSFriendMonitor.Shared.Services.Database.Models;

namespace OSRSFriendMonitor.Shared.Services.Activity;

public interface IActivityStorageService {
    Task StoreActivityUpdateAsync(ActivityUpdate update);
}

public class ActivityStorageService: IActivityStorageService {
    private readonly IDatabaseService _databaseService;
    private readonly ILocationCache _locationCache;

    public ActivityStorageService(IDatabaseService databaseService, ILocationCache locationCache) {
        _databaseService = databaseService;
        _locationCache = locationCache;
    }

    public async Task StoreActivityUpdateAsync(ActivityUpdate update) {
        ActivityUpdate insertedUpdate = await _databaseService.InsertActivityUpdateAsync(update);

        if (insertedUpdate is LocationUpdate locationUpdate) 
        {
            _locationCache.AddLocationUpdate(
                new CachedLocationUpdate(
                    X: locationUpdate.X, 
                    Y: locationUpdate.Y, 
                    Plane: locationUpdate.Plane,
                    AccountHash: locationUpdate.AccountHash
                )
            );
        }
    }
}
