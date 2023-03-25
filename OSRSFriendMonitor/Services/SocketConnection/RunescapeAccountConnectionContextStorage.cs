using OSRSFriendMonitor.Services.SocketConnection.Messages;
using System.Collections.Concurrent;

namespace OSRSFriendMonitor.Services.SocketConnection;

public interface IRunescapeAccountContextStorage
{
    RunescapeAccountContext? GetContext(long accountHash);
    void AddNewContext(long accountHash);
    void RemoveContext(long accountHash);

    IList<long> GetConnectedAccounts();
    RunescapeAccountContext? AtomicallyUpdateContext(long accountHash, Func<RunescapeAccountContext, RunescapeAccountContext> updater);
}

public record struct RunescapeAccountContext(
    LocationUpdateSpeed LocationUpdateSpeed,
    ulong LastLocationPushToClientTick,
    ulong LastLocationUpdateSpeedChangeTick
);

public class RunescapeAccountContextStorage: IRunescapeAccountContextStorage
{
    private readonly ConcurrentDictionary<long, RunescapeAccountContext> _connectedAccounts;

    public RunescapeAccountContextStorage()
    {
        _connectedAccounts = new();
    }

    public IList<long> GetConnectedAccounts()
    {
        return _connectedAccounts.Keys.ToList();
    }

    public void AddNewContext(long accountHash)
    {
        RunescapeAccountContext context = new(LocationUpdateSpeed.Slow, 0, 0);
        _connectedAccounts.TryAdd(accountHash, context);
    }

    public void RemoveContext(long accountHash)
    {
        _connectedAccounts.TryRemove(accountHash, out _);
    }

    public RunescapeAccountContext? GetContext(long accountHash)
    {
        if (_connectedAccounts.TryGetValue(accountHash, out var context)) {
            return context;
        } 
        else
        {
            return null;
        }
    }

    public RunescapeAccountContext? AtomicallyUpdateContext(long accountHash, Func<RunescapeAccountContext, RunescapeAccountContext> updater)
    {
        bool success = false;
        int attemptCount = 0;

        while (!success && attemptCount < 3)
        {
            if (!_connectedAccounts.TryGetValue(accountHash, out var existingContext))
            {
                // Doesn't exist at all.
                return null;
            }

            RunescapeAccountContext newContext = updater(existingContext);

            success = _connectedAccounts.TryUpdate(accountHash, newContext, existingContext);

            if (success)
            {
                return newContext;
            }

            attemptCount++;
        }

        return null;
    }
}
