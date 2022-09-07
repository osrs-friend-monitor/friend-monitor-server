using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSRSFriendMonitor.Account.Models;
using OSRSFriendMonitor.Services.Database;
using OSRSFriendMonitor.Services.Database.Models;
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

        UserAccount newAccount = UserAccount.Create(id: userId);

        await _databaseService.CreateAccount(newAccount);

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

        await _databaseService.CreateOrUpdateRunescapeAccount(
            userId,
            new(AccountHash: model.AccountHash, DisplayName: model.DisplayName)
        );

        return Ok();
    }
}
