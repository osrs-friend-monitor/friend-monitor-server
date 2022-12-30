﻿using OSRSFriendMonitor.Services.SocketConnection.Messages;
using System.Collections.Concurrent;

namespace OSRSFriendMonitor.Services.SocketConnection;

public interface IRunescapeAccountContextStorage
{
    RunescapeAccountContext? GetContext(string accountHash);
    void AddNewContext(string accountHash);
    void RemoveContext(string accountHash);

    IList<string> GetConnectedAccounts();
    RunescapeAccountContext? AtomicallyUpdateContext(string accountHash, Func<RunescapeAccountContext, RunescapeAccountContext> updater);
}

public record struct RunescapeAccountContext(
    LocationUpdateSpeed LocationUpdateSpeed,
    ulong LastLocationPushToClientTick,
    ulong LastLocationUpdateSpeedChangeTick
);

public class RunescapeAccountContextStorage: IRunescapeAccountContextStorage
{
    private readonly ConcurrentDictionary<string, RunescapeAccountContext> _connectedAccounts;

    public RunescapeAccountContextStorage()
    {
        _connectedAccounts = new();
    }

    public IList<string> GetConnectedAccounts()
    {
        return _connectedAccounts.Keys.ToList();
    }

    public void AddNewContext(string accountHash)
    {
        RunescapeAccountContext context = new(LocationUpdateSpeed.Slow, 0, 0);
        _connectedAccounts.TryAdd(accountHash, context);
    }

    public void RemoveContext(string identifier)
    {
        _connectedAccounts.TryRemove(identifier, out _);
    }

    public RunescapeAccountContext? GetContext(string accountHash)
    {
        if (_connectedAccounts.TryGetValue(accountHash, out var context)) {
            return context;
        } 
        else
        {
            return null;
        }
    }

    public RunescapeAccountContext? AtomicallyUpdateContext(string accountHash, Func<RunescapeAccountContext, RunescapeAccountContext> updater)
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