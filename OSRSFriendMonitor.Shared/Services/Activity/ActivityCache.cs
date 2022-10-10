using System.Text.Json;
using OSRSFriendMonitor.Shared.Services.Cache;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Text.Json.Serialization;
using System.Reflection.Metadata.Ecma335;
using StackExchange.Redis;

namespace OSRSFriendMonitor.Shared.Services.Activity;

[JsonSerializable(typeof(CachedLocationUpdateStruct))]

public partial class ActivityCacheJsonContext: JsonSerializerContext { }

public record struct CachedLocationUpdateStruct(
    int X,
    int Y,
    int Plane,
    RunescapeAccountIdentifier RunescapeAccountIdentifier
);

public interface ILocationCache
{
    public void AddLocationUpdate(CachedLocationUpdateStruct update);
    Task<IDictionary<RunescapeAccountIdentifier, CachedLocationUpdateStruct>> GetLocationUpdatesAsync(IEnumerable<RunescapeAccountIdentifier> runescapeAccountIdentifiers);
}

public class ActivityCache : ILocationCache
{
    private readonly IRemoteCache _cache;

    public ActivityCache(IRemoteCache cache)
    {
        _cache = cache;
    }

    public void AddLocationUpdate(CachedLocationUpdateStruct update)
    {
        _cache.SetValueWithoutWaiting(
            new(
                update.RunescapeAccountIdentifier.CombinedIdentifier(),
                JsonSerializer.Serialize(update, ActivityCacheJsonContext.Default.CachedLocationUpdateStruct)
            ), 
            TimeSpan.FromSeconds(2)
        );
    }

    public async Task<IDictionary<RunescapeAccountIdentifier, CachedLocationUpdateStruct>> GetLocationUpdatesAsync(IEnumerable<RunescapeAccountIdentifier> runescapeAccountIdentifiers)
    {
        IEnumerable<KeyValuePair<string, string>> result = await _cache.GetMultipleValuesAsync(runescapeAccountIdentifiers.Select(x => $"location:{x.CombinedIdentifier()}"));

        return result
            .Select(pair =>
            {
                CachedLocationUpdateStruct location = JsonSerializer.Deserialize(
                    pair.Value,
                    ActivityCacheJsonContext.Default.CachedLocationUpdateStruct
                );

                return new KeyValuePair<RunescapeAccountIdentifier, CachedLocationUpdateStruct>(location.RunescapeAccountIdentifier, location);
            })
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
