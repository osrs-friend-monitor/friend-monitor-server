using OSRSFriendMonitor.Services.SocketConnection;
using OSRSFriendMonitor.Services.SocketConnection.Messages;
using OSRSFriendMonitor.Shared.Services.Account;
using OSRSFriendMonitor.Shared.Services.Activity;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OSRSFriendMonitor.Services;

[JsonSerializable(typeof(ServerSocketMessage))]
[JsonSerializable(typeof(FriendLocationUpdate))]
[JsonSerializable(typeof(LocationUpdateMessage))]
[JsonSerializable(typeof(ClientSocketMessage))]
public partial class SocketMessageJsonContext: JsonSerializerContext
{

}


public interface ILocalActivityBroadcaster
{
    public Task<bool> BroadcastActivityAsync(ActivityUpdate update);
    public Task BroadcastLocationUpdatesToConnectedClientsAsync(ulong tick);
}
public class LocalActivityBroadcaster : ILocalActivityBroadcaster
{
    private readonly IRunescapeAccountConnectionService _connectionService;
    private readonly IAccountStorageService _accountStorage;
    private readonly ILocationCache _locationCache;
    private readonly IRunescapeAccountContextStorage _accountContextStorage;

    private readonly SocketMessageJsonContext _jsonContext;

    public LocalActivityBroadcaster(IRunescapeAccountConnectionService connectionService, IAccountStorageService accountStorage, ILocationCache locationCache, IRunescapeAccountContextStorage accountContextStorage, SocketMessageJsonContext jsonContext)
    {
        _accountStorage = accountStorage;
        _connectionService = connectionService;
        _locationCache = locationCache;
        _accountContextStorage = accountContextStorage;
        _jsonContext = jsonContext;
    }
    async Task<bool> ILocalActivityBroadcaster.BroadcastActivityAsync(ActivityUpdate update)
    {
        if (update is LocationUpdate)
        {
            return true;
        }
        else if (update is PlayerDeath death)
        {
            RunescapeAccount? account = await _accountStorage.GetRunescapeAccountAsync(update.AccountIdentifier);

            if (account == null)
            {
                return true;
            }

            ServerSocketMessage message = new FriendDeathMessage(
                X: death.X,
                Y: death.Y,
                Plane: death.Plane,
                DisplayName: account.DisplayName,
                AccountHash: account.AccountIdentifier.AccountHash
            );

            IList<Task> tasks = new List<Task>();

            foreach (RunescapeAccountIdentifier identifier in account.Friends)
            {
                Task task = _connectionService.SendMessageToAccountConnectionAsync(identifier, message, CancellationToken.None);

                tasks.Add(task);
            }

            tasks.Add(_connectionService.SendMessageToAccountConnectionAsync(account.AccountIdentifier, message, CancellationToken.None));

            await Task.WhenAll(tasks);

            return true;
        }

        throw new NotImplementedException();
    }

    public async Task BroadcastLocationUpdatesToConnectedClientsAsync(ulong tick)
    {
        IList<RunescapeAccountIdentifier> onlineAccountsThatNeedUpdates = new List<RunescapeAccountIdentifier>();

        foreach (RunescapeAccountIdentifier identifier in _accountContextStorage.GetConnectedAccounts())
        {
            if (_accountContextStorage.AtomicallyUpdateContext(identifier, existingContext => RunescapeAccountContextProcessor.ProcessContext(tick, existingContext)) is not RunescapeAccountContext context)
            {
                continue;
            }

            if (RunescapeAccountContextProcessor.ShouldSendLocationUpdateToClient(tick, context))
            {
                Debug.WriteLine($"account {identifier} needs update");
                onlineAccountsThatNeedUpdates.Add(identifier);

                _accountContextStorage.AtomicallyUpdateContext(
                    identifier, 
                    existingContext => existingContext with { LastLocationPushToClientTick = tick }
                );

            }
        }

        var onlineRunescapeAccounts = await _accountStorage.GetRunescapeAccountsAsync(onlineAccountsThatNeedUpdates);

        IList<RunescapeAccountIdentifier> allFriendAccountIdentifiers = onlineRunescapeAccounts.Values.SelectMany(account => account.Friends).Concat(onlineAccountsThatNeedUpdates).ToList();

        IDictionary<RunescapeAccountIdentifier, CachedLocationUpdate> locations = await _locationCache.GetLocationUpdatesAsync(allFriendAccountIdentifiers);

        IList<RunescapeAccountIdentifier> friendAccountsWeNeedNamesFor = new List<RunescapeAccountIdentifier>(locations.Count);

        foreach (var accountIdentifier in allFriendAccountIdentifiers)
        {
            if (!locations.ContainsKey(accountIdentifier))
            {
                continue;
            }

            friendAccountsWeNeedNamesFor.Add(accountIdentifier);
        }

        IDictionary<RunescapeAccountIdentifier, RunescapeAccount> friendRunescapeAccounts = await _accountStorage.GetRunescapeAccountsAsync(friendAccountsWeNeedNamesFor);

        IDictionary<RunescapeAccountIdentifier, LocationUpdateMessage> updates = new Dictionary<RunescapeAccountIdentifier, LocationUpdateMessage>();

        IList<Task> tasks = new List<Task>();

        foreach (var onlineAccountIdentifier in onlineAccountsThatNeedUpdates)
        {
            if (!onlineRunescapeAccounts.TryGetValue(onlineAccountIdentifier, out RunescapeAccount? onlineAccount) || onlineAccount is null)
            {
                continue;
            }

            IList<FriendLocationUpdate> friendUpdates = new List<FriendLocationUpdate>();

            foreach (var friendAccountIdentifier in onlineAccount.Friends)
            {
                if (!locations.TryGetValue(friendAccountIdentifier, out CachedLocationUpdate? location) || location is null) { continue; }
                if (!friendRunescapeAccounts.TryGetValue(friendAccountIdentifier, out RunescapeAccount? friendAccount) || friendAccount is null) { continue; }

                FriendLocationUpdate update = new(
                    X: location.X,
                    Y: location.Y,
                    Plane: location.Plane,
                    DisplayName: friendAccount.DisplayName,
                    AccountHash: friendAccount.AccountIdentifier.AccountHash
                );

                friendUpdates.Add(update);
            }

            if (locations.TryGetValue(onlineAccountIdentifier, out CachedLocationUpdate? cachedSelfLocation) && cachedSelfLocation is not null) 
            {
                FriendLocationUpdate locationOfSelf = new(
                    X: cachedSelfLocation.X,
                    Y: cachedSelfLocation.Y,
                    Plane: cachedSelfLocation.Plane,
                    DisplayName: onlineAccount.DisplayName,
                    AccountHash: onlineAccount.AccountIdentifier.AccountHash
                );

                friendUpdates.Add(locationOfSelf);
            }

            updates[onlineAccountIdentifier] = new(friendUpdates.ToArray());

            ServerSocketMessage message = new LocationUpdateMessage(friendUpdates.ToArray());

            Task task = Task.Run(async () =>
            {
                try
                {
                    await _connectionService.SendMessageToAccountConnectionAsync(onlineAccountIdentifier, message, CancellationToken.None);
                }
                catch { }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }
}
