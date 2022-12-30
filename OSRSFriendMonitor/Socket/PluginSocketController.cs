using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using OSRSFriendMonitor.Services.SocketConnection;
using OSRSFriendMonitor.Shared.Services.Account;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Security.Claims;

namespace OSRSFriendMonitor.Socket;

public sealed class HttpConnectAttribute : HttpMethodAttribute
{
    private static readonly IEnumerable<string> _supportedMethods = new[] { "CONNECT" };

    public HttpConnectAttribute()
        : base(_supportedMethods)
    {
    }

    public HttpConnectAttribute(string template)
        : base(_supportedMethods, template)
    {
    }
}

[ApiController]
[Route("api/socket")]
public class PluginSocketController : ControllerBase
{
    private readonly SocketConnectionManager _socketConnectionManager;
    private readonly IAccountStorageService _accountStorage;
    public PluginSocketController(SocketConnectionManager socketConnectionManager, IAccountStorageService accountStorage)
    {
        _socketConnectionManager = socketConnectionManager;
        _accountStorage = accountStorage;
    }

    [HttpGet("{runescapeAccountHash}")]
    [HttpConnect("{runescapeAccountHash}")]
    public async Task Get(string runescapeAccountHash)
    {
        if (!(HttpContext.User.Identity?.IsAuthenticated ?? false))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        else if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            string? accountIdFromIdentity = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


            if (accountIdFromIdentity is null)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            if (await _accountStorage.GetRunescapeAccountAsync(runescapeAccountHash) is null)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            await _socketConnectionManager.HandleConnectionAsync(runescapeAccountHash, webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    [HttpGet("connections")]
    public int GetSocketCount()
    {
        return _socketConnectionManager._liveConnections.Count;
    }
}

