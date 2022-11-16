using OSRSFriendMonitor.Services.SocketConnection.Messages;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Collections.Concurrent;

namespace OSRSFriendMonitor.Services.SocketConnection;

public interface IRunescapeAccountContextStorage
{
    RunescapeAccountContext? GetContext(RunescapeAccountIdentifier identifier);
    void AddNewContext(RunescapeAccountIdentifier identifier);
    void RemoveContext(RunescapeAccountIdentifier identifier);

    IList<RunescapeAccountIdentifier> GetConnectedAccounts();
    RunescapeAccountContext? AtomicallyUpdateContext(RunescapeAccountIdentifier identifier, Func<RunescapeAccountContext, RunescapeAccountContext> updater);
}

public record struct RunescapeAccountContext(
    LocationUpdateSpeed LocationUpdateSpeed,
    ulong LastLocationPushToClientTick,
    ulong LastLocationUpdateSpeedChangeTick
);

public class RunescapeAccountContextStorage: IRunescapeAccountContextStorage
{
    private readonly ConcurrentDictionary<RunescapeAccountIdentifier, RunescapeAccountContext> _connectedAccounts;

    public RunescapeAccountContextStorage()
    {
        _connectedAccounts = new();
    }

    public IList<RunescapeAccountIdentifier> GetConnectedAccounts()
    {
        return _connectedAccounts.Keys.ToList();
    }

    public void AddNewContext(RunescapeAccountIdentifier identifier)
    {
        RunescapeAccountContext context = new(LocationUpdateSpeed.SLOW, 0, 0);
        _connectedAccounts.TryAdd(identifier, context);
    }

    public void RemoveContext(RunescapeAccountIdentifier identifier)
    {
        _connectedAccounts.TryRemove(identifier, out _);
    }

    public RunescapeAccountContext? GetContext(RunescapeAccountIdentifier identifier)
    {
        if (_connectedAccounts.TryGetValue(identifier, out var context)) {
            return context;
        } 
        else
        {
            return null;
        }
    }

    public RunescapeAccountContext? AtomicallyUpdateContext(RunescapeAccountIdentifier identifier, Func<RunescapeAccountContext, RunescapeAccountContext> updater)
    {
        bool success = false;
        int attemptCount = 0;

        while (!success && attemptCount < 3)
        {
            if (!_connectedAccounts.TryGetValue(identifier, out var existingContext))
            {
                // Doesn't exist at all.
                return null;
            }

            RunescapeAccountContext newContext = updater(existingContext);

            success = _connectedAccounts.TryUpdate(identifier, newContext, existingContext);

            if (success)
            {
                return newContext;
            }

            attemptCount++;
        }

        return null;
    }
}
