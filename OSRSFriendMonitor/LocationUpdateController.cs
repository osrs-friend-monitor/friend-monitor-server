using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

public record LocationUpdate(
    int X,
    int Y,
    int Plane,
    string AccountHash
);

namespace OSRSFriendMonitor
{
    [Route("api/location")]
    [ApiController]
    public class LocationUpdateController : ControllerBase
    {
        private LocationUpdateNotifier _notifier;

        public LocationUpdateController(LocationUpdateNotifier notifier)
        {
            _notifier = notifier;
        }

        [HttpPost]
        public async void Post([FromBody] LocationUpdate value)
        {
            await _notifier.NotifyOnlineFriendsOfLocationUpdateAsync(value);
        }
    }
}
