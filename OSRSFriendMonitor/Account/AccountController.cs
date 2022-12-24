using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSRSFriendMonitor.Account.Models;
using OSRSFriendMonitor.Shared.Services.Database;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Security.Claims;

namespace OSRSFriendMonitor.Account;

[Route("api/account")]
[ApiController]
[Authorize]
public sealed class AccountController : ControllerBase
{
    private readonly IDatabaseService _databaseService;

    public AccountController(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    [HttpPost]
    public async Task<IActionResult> Create()
    {
        string? userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId is null)
        {
            return Unauthorized();
        }

        UserAccount newAccount = new(userId);

        await _databaseService.CreateAccountAsync(newAccount);

        return Ok();
    }

    [HttpPost("runescape")]
    public async Task<IActionResult> UpdateRunescapeAccount([FromBody] CreateOrUpdateRunescapeAccountModel model)
    {
        string? userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId is null)
        {
            return Unauthorized();
        }
        // TODO;
        // await _databaseService.CreateOrUpdateRunescapeAccountAsync(new(userId, model.AccountHash), model.DisplayName);

        return Ok();
    }
}
