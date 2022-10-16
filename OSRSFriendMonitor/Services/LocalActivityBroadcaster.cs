using OSRSFriendMonitor.Shared.Services.Account;
using OSRSFriendMonitor.Shared.Services.Activity;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OSRSFriendMonitor.Services;

[JsonSerializable(typeof(SocketMessage))]
public partial class SocketMessageJsonContext: JsonSerializerContext
{

}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(LocationUpdateMessage), "LOCATION")]
public abstract record SocketMessage();
public record LocationUpdateMessage(
    FriendLocationUpdate[] Updates
): SocketMessage();

public record struct FriendLocationUpdate(
    int X,
    int Y,
    int Plane,
    string DisplayName
);

public interface ILocalActivityBroadcaster
{
    public Task<bool> BroadcastActivityAsync(ActivityUpdate update);
    public Task BroadcastLocationUpdatesToConnectedClientsAsync();
}
public class LocalActivityBroadcaster : ILocalActivityBroadcaster
{
    private readonly SocketConnectionManager _connectionManager;
    private readonly IAccountStorageService _accountStorage;
    private readonly ILocationCache _locationCache;

    private readonly SocketMessageJsonContext _jsonContext;

    public LocalActivityBroadcaster(SocketConnectionManager connectionManager, IAccountStorageService accountStorage, ILocationCache locationCache)
    {
        _accountStorage = accountStorage;
        _connectionManager = connectionManager;
        _locationCache = locationCache;
    
        _jsonContext = new SocketMessageJsonContext(new JsonSerializerOptions() { DictionaryKeyPolicy = JsonNamingPolicy.CamelCase });
    }
    async Task<bool> ILocalActivityBroadcaster.BroadcastActivityAsync(ActivityUpdate update)
    {
        if (update is LocationUpdate)
        {
            return true;
        }

        throw new NotImplementedException();
    }

    public async Task BroadcastLocationUpdatesToConnectedClientsAsync()
    {
        IList<RunescapeAccountIdentifier> onlineAccounts = _connectionManager.GetConnectedAccounts().ToList();
        var onlineRunescapeAccounts = await _accountStorage.GetRunescapeAccountsAsync(onlineAccounts);

        IList<RunescapeAccountIdentifier> allFriendAccountIdentifiers = onlineRunescapeAccounts.Values.SelectMany(account => account.Friends).ToList();

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

        foreach (var onlineAccountIdentifier in onlineAccounts)
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
                    DisplayName: friendAccount.DisplayName
                );

                friendUpdates.Add(update);
            }

            updates[onlineAccountIdentifier] = new(friendUpdates.ToArray());

            SocketMessage message = new LocationUpdateMessage(friendUpdates.ToArray());

            string json = JsonSerializer.Serialize(message, _jsonContext.SocketMessage);

            Task task = Task.Run(async () =>
            {
                try
                {
                    await _connectionManager.SendMessageToConnectionAsync(onlineAccountIdentifier, json);
                }
                catch { }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

}
