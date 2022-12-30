using System.Diagnostics;
using System.Text.Json;
using OSRSFriendMonitor.Services.SocketConnection.Messages;
using OSRSFriendMonitor.Shared.Services.Database.Models;

namespace OSRSFriendMonitor.Services.SocketConnection;

public interface IRunescapeAccountConnectionService
{
    Task SendMessageToAccountConnectionAsync(string accountHash, ServerSocketMessage message, CancellationToken cancellationToken);
    IList<string> GetConnectedAccounts();
}
public class RunescapeAccountConnectionService: IRunescapeAccountConnectionService
{
    private readonly IRunescapeAccountContextStorage _storage;
    private readonly SocketConnectionManager _connectionManager;
    private readonly ITickAccessor _tickAccessor;
    private readonly SocketMessageJsonContext _jsonContext;

    public RunescapeAccountConnectionService(IRunescapeAccountContextStorage storage, SocketConnectionManager connectionManager, ITickAccessor tickAccessor, SocketMessageJsonContext jsonContext)
    {
        _storage = storage;
        _connectionManager = connectionManager;
        _tickAccessor = tickAccessor;
        _jsonContext = jsonContext;

        _connectionManager.messageReceived = MessageReceived;
        _connectionManager.accountConnected = AccountConnected;
        _connectionManager.accountDisconnected = AccountDisconnected;
    }

    public IList<string> GetConnectedAccounts()
    {
        return _storage.GetConnectedAccounts();
    }

    public async Task SendMessageToAccountConnectionAsync(string accountHash, ServerSocketMessage message, CancellationToken cancellationToken)
    {
        string messageText = JsonSerializer.Serialize(message, _jsonContext.ServerSocketMessage);

        await _connectionManager.SendMessageToConnectionAsync(accountHash, messageText, cancellationToken);
    }

    private void AccountConnected(string accountHash)
    {
        _storage.AddNewContext(accountHash);
    }

    private void AccountDisconnected(string accountHash)
    {
        _storage.RemoveContext(accountHash);
    }

    private void MessageReceived(string accountHash, string messageText)
    {
        try
        {
            ClientSocketMessage? message = JsonSerializer.Deserialize(messageText, _jsonContext.ClientSocketMessage);

            if (message is null)
            {
                return;
            }

            if (message is LocationUpdateSpeedMessage speedMessage)
            {
                _storage.AtomicallyUpdateContext(
                    accountHash,
                    existingContext => existingContext with
                    {
                        LocationUpdateSpeed = speedMessage.Speed,
                        LastLocationUpdateSpeedChangeTick = _tickAccessor.GetTick()
                    }
                );
            }
            else
            {
                // ??
            }

        } catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }
}
