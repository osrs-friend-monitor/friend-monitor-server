using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks.Dataflow;

namespace OSRSFriendMonitor;

public record RunescapeAccountIdentifier(String AccountHash);

public class LiveConnectionManager
{
    public ConcurrentDictionary<RunescapeAccountIdentifier, WebSocket> _liveConnections = new();

    public WebSocket? GetSocket(RunescapeAccountIdentifier identifier)
    {
        WebSocket? result = null;

        try
        {
            _liveConnections.TryGetValue(identifier, out result);
        }
        catch {}

        return result;
    }

    public async Task SendMessageToAccountAsync(RunescapeAccountIdentifier accountIdentifier, string message)
    {
        WebSocket? socket = GetSocket(accountIdentifier);

        if (socket is null || socket.State != WebSocketState.Open)
        {
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(message);

        ArraySegment<byte> buffer = new(bytes, 0, message.Length);

        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task HandleConnectionAsync(RunescapeAccountIdentifier identifier, WebSocket socket)
    {
        using (socket)
        {
            _liveConnections.TryAdd(identifier, socket);

            var countBeforeRemove = _liveConnections.Count;

            if (countBeforeRemove % 100 == 0)
            {
                Debug.WriteLine($"Connection count after add: {countBeforeRemove}");
            }

            try
            {
                while (socket.State is WebSocketState.Open)
                {
                    string? message = await GetSingleMessageAsync(socket);

                    if (message is null)
                    {
                        break;
                    }
                }

                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            
            _ = _liveConnections.TryRemove(identifier, out _);
            var countAfterRemove = _liveConnections.Count;

            if (countAfterRemove % 100 == 0)
            {
                Debug.WriteLine($"Connection count after remove: {countAfterRemove}");
            }
        }
    }

    private static async Task<string?> GetSingleMessageAsync(WebSocket socket)
    {
        try
        {
            using (var ms = new MemoryStream())
            {
                var buffer = new ArraySegment<byte>(new byte[8192]);

                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.CloseStatus is not null)
                    {
                        break;
                    }
                    await ms.WriteAsync(buffer.Array!, buffer.Offset, result.Count, CancellationToken.None);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return "wowee";
                }
                else if (result.MessageType != WebSocketMessageType.Text)
                {
                    return null;
                }
                else
                {
                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                    {
                        string text = await reader.ReadToEndAsync();
                        await socket.SendAsync(Encoding.ASCII.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);
                        return text;
                    }

                }
            }
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}
