using System.Text.Json;
using OSRSFriendMonitor.Shared.Services.Cache;
using System.Text.Json.Serialization;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace OSRSFriendMonitor.Shared.Services.Activity;

[JsonSerializable(typeof(CachedLocationUpdate))]
public partial class ActivityCacheJsonContext: JsonSerializerContext { }

public sealed record CachedLocationUpdate(
    int X,
    int Y,
    int Plane,
    long AccountHash
);

public interface ILocationCache
{
    public void AddLocationUpdate(CachedLocationUpdate update);
    Task<IDictionary<long, CachedLocationUpdate>> GetLocationUpdatesAsync(IEnumerable<long> accountHashes);
}

public class ActivityCache : ILocationCache
{
    private readonly IRemoteCache _remote;
    private readonly ILogger<ActivityCache> _logger;

    public ActivityCache(IRemoteCache remote, ILogger<ActivityCache> logger)
    {
        _remote = remote;
        _logger = logger;
    }

    public void AddLocationUpdate(CachedLocationUpdate update)
    {
        TimeSpan expiration = TimeSpan.FromSeconds(12);
        string key = $"location:{update.AccountHash}";

        _remote.SetValueWithoutWaiting(
            new(
                key,
                JsonSerializer.Serialize(update, ActivityCacheJsonContext.Default.CachedLocationUpdate)
            ), 
            expiration
        );
    }

    public async Task<IDictionary<long, CachedLocationUpdate>> GetLocationUpdatesAsync(IEnumerable<long> accountHashes)
    {
        RedisValue[] cachedResults = await _remote.GetMultipleValuesAsync(accountHashes.Select(x => $"location:{x}"));

        IDictionary<long, CachedLocationUpdate> results = new Dictionary<long, CachedLocationUpdate>(capacity: cachedResults.Length);

        for (int index = 0; index < cachedResults.Length; index++)
        {
            RedisValue cachedResult = cachedResults[index];

            if (cachedResult.IsNull)
            {
                continue;
            }
            try
            {
                CachedLocationUpdate? location = JsonSerializer.Deserialize(
                    cachedResult!,
                    ActivityCacheJsonContext.Default.CachedLocationUpdate
                );

                if (location is null) continue;

                results[location.AccountHash] = location;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize cached location update");
            }

        }

        return results;
    }
}
