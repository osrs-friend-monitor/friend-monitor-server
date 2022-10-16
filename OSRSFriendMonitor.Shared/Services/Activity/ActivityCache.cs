using System.Text.Json;
using OSRSFriendMonitor.Shared.Services.Cache;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Text.Json.Serialization;
using System.Reflection.Metadata.Ecma335;
using StackExchange.Redis;

namespace OSRSFriendMonitor.Shared.Services.Activity;

[JsonSerializable(typeof(CachedLocationUpdate))]
public partial class ActivityCacheJsonContext: JsonSerializerContext { }

public sealed record CachedLocationUpdate(
    int X,
    int Y,
    int Plane,
    RunescapeAccountIdentifier RunescapeAccountIdentifier
);

public interface ILocationCache
{
    public void AddLocationUpdate(CachedLocationUpdate update);
    Task<IDictionary<RunescapeAccountIdentifier, CachedLocationUpdate>> GetLocationUpdatesAsync(IList<RunescapeAccountIdentifier> runescapeAccountIdentifiers);
}

public class ActivityCache : ILocationCache
{
    private readonly IRemoteCache _remote;
    public ActivityCache(IRemoteCache remote)
    {
        _remote = remote;
    }

    public void AddLocationUpdate(CachedLocationUpdate update)
    {
        TimeSpan expiration = TimeSpan.FromSeconds(2);
        string key = $"location:{update.RunescapeAccountIdentifier.CombinedIdentifier()}";

        _remote.SetValueWithoutWaiting(
            new(
                key,
                JsonSerializer.Serialize(update, ActivityCacheJsonContext.Default.CachedLocationUpdate)
            ), 
            expiration
        );
    }

    public async Task<IDictionary<RunescapeAccountIdentifier, CachedLocationUpdate>> GetLocationUpdatesAsync(IList<RunescapeAccountIdentifier> runescapeAccountIdentifiers)
    {
        RedisValue[] cachedResults = await _remote.GetMultipleValuesAsync(runescapeAccountIdentifiers.Select(x => $"location:{x.CombinedIdentifier()}"));

        IDictionary<RunescapeAccountIdentifier, CachedLocationUpdate> results = new Dictionary<RunescapeAccountIdentifier, CachedLocationUpdate>();

        for (int index = 0; index < cachedResults.Length; index++)
        {
            RedisValue cachedResult = cachedResults[index];

            if (cachedResult.IsNull)
            {
                continue;
            }

            CachedLocationUpdate? location = JsonSerializer.Deserialize(
                cachedResult!,
                ActivityCacheJsonContext.Default.CachedLocationUpdate
            );

            if (location is null) continue;

            results[location.RunescapeAccountIdentifier] = location;
        }

        return results;
    }
}
