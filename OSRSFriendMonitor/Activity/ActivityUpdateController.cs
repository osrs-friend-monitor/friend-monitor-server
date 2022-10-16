using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSRSFriendMonitor.Activity.Models;
using OSRSFriendMonitor.Services;
using System.Diagnostics;
using System.Security.Claims;

namespace OSRSFriendMonitor.Activity;


[Route("api/activity")]
[ApiController]
[Authorize]
public class ActivityUpdateController : ControllerBase
{
    private readonly IActivityProcessor _activityProcessor;
    public ActivityUpdateController(IActivityProcessor activityProcessor)
    {
        _activityProcessor = activityProcessor;
    }


    [HttpPost]
    public async Task Post([FromBody] ActivityUpdate update)
    {
        Debug.WriteLine(update);
        //await _notifier.NotifyOnlineFriendsOfLocationUpdateAsync(value);



    }
}
