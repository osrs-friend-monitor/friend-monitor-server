using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

namespace OSRSFriendMonitor.Services.SocketConnection;

public class SocketConnectionManager
{
    public ConcurrentDictionary<long, WebSocket> _liveConnections = new();
    private readonly ILogger<SocketConnectionManager> _logger;

    public Action<long, string>? messageReceived;
    public Action<long>? accountConnected;
    public Action<long>? accountDisconnected;

    public SocketConnectionManager(ILogger<SocketConnectionManager> logger)
    {
        _logger = logger;
    }

    public ICollection<long> GetConnectedAccounts()
    {
        return _liveConnections.Keys;
    }

    public WebSocket? GetSocket(long accountHash)
    {
        WebSocket? result = null;

        try
        {
            _liveConnections.TryGetValue(accountHash, out result);
        }
        catch { }

        return result;
    }

    public async Task SendMessageToConnectionAsync(long accountHash, string message, CancellationToken cancellationToken = default)
    {
        WebSocket? socket = GetSocket(accountHash);

        if (socket is null || socket.State != WebSocketState.Open)
        {
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(message);

        ArraySegment<byte> buffer = new(bytes, 0, message.Length);

        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
    }

    public async Task HandleConnectionAsync(long accountHash, WebSocket socket)
    {
        accountConnected?.Invoke(accountHash);

        using (socket)
        {
            _liveConnections.TryAdd(accountHash, socket);

            var countBeforeRemove = _liveConnections.Count;

            if (countBeforeRemove % 20 == 0)
            {
                _logger.LogInformation("Connection count after add: {countBeforeRemove}", countBeforeRemove);
            }

            try
            {
                var buffer = new byte[8192];
                WebSocketReceiveResult socketReceiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (socketReceiveResult.MessageType != WebSocketMessageType.Close)
                {
                    string message = await ReceiveMessagePayloadAsync(socketReceiveResult, buffer, socket);

                    messageReceived?.Invoke(accountHash, message);

                    socketReceiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                if (socketReceiveResult.CloseStatus is not null)
                {
                    await socket.CloseAsync(socketReceiveResult.CloseStatus.Value, socketReceiveResult.CloseStatusDescription, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            _ = _liveConnections.TryRemove(accountHash, out _);

            accountDisconnected?.Invoke(accountHash);

            var countAfterRemove = _liveConnections.Count;

            if (countAfterRemove % 20 == 0)
            {
                _logger.LogInformation("Connection count after remove: {countAfterRemove}", countAfterRemove);
            }
        }
    }

    private static async Task<string> ReceiveMessagePayloadAsync(WebSocketReceiveResult webSocketReceiveResult, byte[] buffer, WebSocket socket)
    {
        byte[] messagePayload;

        if (webSocketReceiveResult.EndOfMessage)
        {
            messagePayload = new byte[webSocketReceiveResult.Count];
            Array.Copy(buffer, messagePayload, webSocketReceiveResult.Count);
        }
        else
        {
            using var messagePayloadStream = new MemoryStream();

            messagePayloadStream.Write(buffer, 0, webSocketReceiveResult.Count);
            while (!webSocketReceiveResult.EndOfMessage)
            {
                webSocketReceiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                messagePayloadStream.Write(buffer, 0, webSocketReceiveResult.Count);
            }

            messagePayload = messagePayloadStream.ToArray();
        }

        return Encoding.UTF8.GetString(messagePayload);
    }
}
