using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace OSRSFriendMonitor;

public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly LiveConnectionManager _connectionManager;

    public WebSocketMiddleware(RequestDelegate next, LiveConnectionManager connectionManager)
    {
        _next = next;
        _connectionManager = connectionManager;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        if (httpContext.Request.Path != "/plugin-socket")
        {
            await _next.Invoke(httpContext);
            return;
        }

        if (!httpContext.WebSockets.IsWebSocketRequest)
        {
            httpContext.Response.ContentType = "text/plain";
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await httpContext.Response.StartAsync();
            return;
        }

        string accountHash = httpContext.Request.Query["account-hash"];

        if (accountHash == String.Empty)
        {
            httpContext.Response.ContentType = "text/plain";
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await httpContext.Response.StartAsync();
            return;
        }

        CancellationToken ct = httpContext.RequestAborted;
        WebSocket currentSocket = await httpContext.WebSockets.AcceptWebSocketAsync();

        RunescapeAccountIdentifier identifier = new(accountHash);

        await _connectionManager.HandleConnectionAsync(identifier, currentSocket);
    }

}

