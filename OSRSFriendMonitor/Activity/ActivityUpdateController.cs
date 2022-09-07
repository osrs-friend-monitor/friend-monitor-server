using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSRSFriendMonitor.Activity.Models;
using System.Security.Claims;

namespace OSRSFriendMonitor.Activity;


[Route("api/activity")]
[ApiController]
[Authorize]
public class ActivityUpdateController : ControllerBase
{
    private readonly LocationUpdateNotifier _notifier;

    public ActivityUpdateController(LocationUpdateNotifier notifier)
    {
        _notifier = notifier;
    }


    [HttpPost]
    public async Task Post([FromBody] ActivityUpdate update)
    {
        //await _notifier.NotifyOnlineFriendsOfLocationUpdateAsync(value);



    }
}
