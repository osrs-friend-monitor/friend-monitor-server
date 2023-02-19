using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSRSFriendMonitor.Account.Models;
using OSRSFriendMonitor.Shared.Services.Account;
using System.Security.Claims;

namespace OSRSFriendMonitor.Account;

[Route("api/account")]
[ApiController]
[Authorize]
public sealed class AccountController : ControllerBase
{
    private readonly IAccountStorageService _accountStorageService;

    public AccountController(IAccountStorageService accountStorageService)
    {
        _accountStorageService = accountStorageService;
    }

    [HttpPost("runescape")]
    public async Task<IActionResult> UpdateRunescapeAccount([FromBody] CreateOrUpdateRunescapeAccountModel model)
    {
        string? userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId is null)
        {
            return Unauthorized();
        }

        await _accountStorageService.CreateRunescapeAccountOrUpdateAsync(
            accountHash: model.AccountHash,
            userId: userId,
            displayName: model.DisplayName,
            previousDisplayName: model.PreviousDisplayName,
            friends: model.Friends.Select(f => Tuple.Create(f.DisplayName, f.PreviousDisplayName)).ToArray()
        );

        return Ok();
    }
}
