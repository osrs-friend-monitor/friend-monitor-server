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

        await _accountStorageService.CreateRunescapeAccountOrUpdateNameAsync(
            accountHash: model.AccountHash,
            userId: userId,
            displayName: model.DisplayName,
            previousDisplayName: model.PreviousDisplayName
        );

        return Ok();
    }
}
