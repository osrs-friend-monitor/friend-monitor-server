using OSRSFriendMonitor.Services.SocketConnection;
using OSRSFriendMonitor.Services.SocketConnection.Messages;
using OSRSFriendMonitor.Shared.Services.Account;
using OSRSFriendMonitor.Shared.Services.Activity;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Diagnostics;
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
            RunescapeAccount? account = await _accountStorage.GetRunescapeAccountAsync(update.AccountHash);
            ValidatedFriendsList? friendsList = await _accountStorage.GetValidatedFriendsListForAccountAsync(update.AccountHash);
            if (account is null || friendsList is null)
            {
                return true;
            }

            ServerSocketMessage message = new FriendDeathMessage(
                X: death.X,
                Y: death.Y,
                Plane: death.Plane,
                DisplayName: account.DisplayName,
                AccountHash: account.AccountHash
            );

            IList<Task> tasks = new List<Task>();

            foreach (ValidatedFriend friend in friendsList.Friends)
            {
                // Not actually friends
                if (friend.AccountHash is null)
                {
                    continue;
                }

                Task task = _connectionService.SendMessageToAccountConnectionAsync((long)friend.AccountHash, message, CancellationToken.None);

                tasks.Add(task);
            }

            tasks.Add(_connectionService.SendMessageToAccountConnectionAsync(account.AccountHash, message, CancellationToken.None));

            await Task.WhenAll(tasks);

            return true;
        }
        else if (update is LevelUp levelUp)
        {
            RunescapeAccount? account = await _accountStorage.GetRunescapeAccountAsync(update.AccountHash);
            ValidatedFriendsList? friendsList = await _accountStorage.GetValidatedFriendsListForAccountAsync(update.AccountHash);
            if (account is null || friendsList is null)
            {
                return true;
            }

            LevelUpMessage message = new(
                levelUp.Skill, 
                levelUp.Level, 
                account.DisplayName, 
                account.AccountHash
            );

            IList<Task> tasks = new List<Task>();

            foreach (ValidatedFriend friend in friendsList.Friends)
            {
                if (friend.AccountHash is null) {
                    continue;
                }

                Task task = _connectionService.SendMessageToAccountConnectionAsync((long)friend.AccountHash, message, CancellationToken.None);

                tasks.Add(task);
            }

            tasks.Add(_connectionService.SendMessageToAccountConnectionAsync(account.AccountHash, message, CancellationToken.None));

            await Task.WhenAll(tasks);

            return true;
        }

        throw new NotImplementedException();
    }

    public async Task BroadcastLocationUpdatesToConnectedClientsAsync(ulong tick)
    {
        IList<long> onlineAccountsThatNeedUpdates = new List<long>();

        foreach (long accountHash in _accountContextStorage.GetConnectedAccounts())
        {
            if (_accountContextStorage.AtomicallyUpdateContext(accountHash, existingContext => RunescapeAccountContextProcessor.ProcessContext(tick, existingContext)) is not RunescapeAccountContext context)
            {
                continue;
            }

            if (RunescapeAccountContextProcessor.ShouldSendLocationUpdateToClient(tick, context))
            {
                Debug.WriteLine($"account {accountHash} needs update");
                onlineAccountsThatNeedUpdates.Add(accountHash);

                _accountContextStorage.AtomicallyUpdateContext(
                    accountHash, 
                    existingContext => existingContext with { LastLocationPushToClientTick = tick }
                );
            }
        }

        var onlineRunescapeAccounts = await _accountStorage.GetRunescapeAccountsAsync(onlineAccountsThatNeedUpdates);

        var friendsListsForOnlineAccounts = await _accountStorage.GetValidatedFriendsListsForAccountsAsync(onlineAccountsThatNeedUpdates);

        IList<long> allFriendAccountHashes = friendsListsForOnlineAccounts.Values
            .SelectMany(friendsList => friendsList)
            .Concat(onlineAccountsThatNeedUpdates)
            .ToList();

        IDictionary<long, CachedLocationUpdate> locations = await _locationCache.GetLocationUpdatesAsync(allFriendAccountHashes);

        IList<long> friendAccountsWeNeedNamesFor = new List<long>(locations.Count);

        foreach (var accountHash in allFriendAccountHashes)
        {
            if (!locations.ContainsKey(accountHash))
            {
                continue;
            }

            friendAccountsWeNeedNamesFor.Add(accountHash);
        }

        IDictionary<long, RunescapeAccount> friendRunescapeAccounts = await _accountStorage.GetRunescapeAccountsAsync(friendAccountsWeNeedNamesFor);

        IDictionary<long, LocationUpdateMessage> updates = new Dictionary<long, LocationUpdateMessage>();

        IList<Task> tasks = new List<Task>();

        foreach (var onlineAccountIdentifier in onlineAccountsThatNeedUpdates)
        {
            if (!onlineRunescapeAccounts.TryGetValue(onlineAccountIdentifier, out RunescapeAccount? onlineAccount) || onlineAccount is null)
            {
                continue;
            }

            if (!friendsListsForOnlineAccounts.TryGetValue(onlineAccountIdentifier, out long[]? friendsList) || friendsList is null) {
                continue;
            }

            IList<FriendLocationUpdate> friendUpdates = new List<FriendLocationUpdate>();

            foreach (var friend in friendsList)
            {
                if (!locations.TryGetValue(friend, out CachedLocationUpdate? location) || location is null) { continue; }
                if (!friendRunescapeAccounts.TryGetValue(friend, out RunescapeAccount? friendAccount) || friendAccount is null) { continue; }

                FriendLocationUpdate update = new(
                    X: location.X,
                    Y: location.Y,
                    Plane: location.Plane,
                    DisplayName: friendAccount.DisplayName,
                    AccountHash: friendAccount.AccountHash
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
                    AccountHash: onlineAccount.AccountHash
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
