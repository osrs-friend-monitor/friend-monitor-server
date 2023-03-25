using StackExchange.Redis;

namespace OSRSFriendMonitor.Shared.Services.Cache;

public interface IRemoteCache
{
    public void AddToSetWithoutWaiting(string key, RedisValue value, DateTime? expiration = null);
    public Task SetHashAsync(string hashKey, string key, string value);
    public Task SetHashAsync(string hashKey, HashEntry[] fields);
    public Task<IEnumerable<HashEntry>> GetAllHashValuesAsync(string hashKey);
    public Task<IEnumerable<string?>> GetHashValuesAsync(string hashKey, string[] keys);
    public Task SetMultipleValuesAsync(IEnumerable<KeyValuePair<string, string>> values);
    public Task<RedisValue[]> GetMultipleValuesAsync(IEnumerable<string> keys);
    public void SetValueWithoutWaiting(KeyValuePair<string, string> pair, TimeSpan? expiration = null);
    public Task<string?> GetValueAsync(string key);
}

public class RedisCache: IRemoteCache
{
    private readonly IConnectionMultiplexer _multiplexer;

    public RedisCache(IConnectionMultiplexer multiplexer)
    {
        _multiplexer = multiplexer;
    }

    public async Task SetHashAsync(string hashKey, string key, string value)
    {
        await _multiplexer.GetDatabase().HashSetAsync(hashKey, key, value);
    }

    public async Task SetHashAsync(string hashKey, HashEntry[] fields)
    {
        await _multiplexer.GetDatabase().HashSetAsync(hashKey, fields);
    }

    public async Task<IEnumerable<HashEntry>> GetAllHashValuesAsync(string hashKey)
    {
        return await _multiplexer.GetDatabase().HashGetAllAsync(hashKey);
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var value = await _multiplexer.GetDatabase().StringGetAsync(key);

        if (value.IsNull)
        {
            return null;
        } 
        else
        {
            return value.ToString();
        }
    }

    public void AddToSetWithoutWaiting(string key, RedisValue value, DateTime? expiration = null)
    {
        ITransaction t = _multiplexer.GetDatabase().CreateTransaction();
        t.SetAddAsync((RedisKey)key, value);
        t.KeyExpireAsync((RedisKey)key, expiration);
        t.Execute(CommandFlags.FireAndForget);
    }

    public void SetValueWithoutWaiting(KeyValuePair<string, string> pair, TimeSpan? expiration = null)
    {
        _multiplexer.GetDatabase().StringSet(
            (RedisKey)pair.Key, 
            new RedisValue(pair.Value), 
            expiry: expiration, 
            flags: CommandFlags.FireAndForget
        );
    }

    public async Task SetMultipleValuesAsync(IEnumerable<KeyValuePair<string, string>> values)
    {
        var redisKeysAndValues = values.Select(pair =>
        {
            return new KeyValuePair<RedisKey, RedisValue>(pair.Key, pair.Value);
        })!.ToArray()!;

        await _multiplexer.GetDatabase().StringSetAsync(redisKeysAndValues);
    }

    public async Task<RedisValue[]> GetMultipleValuesAsync(IEnumerable<string> keys)
    {     
        RedisValue[] values = await _multiplexer.GetDatabase().StringGetAsync(keys.Select(x => (RedisKey)x).ToArray());

        return values;
    }

    public async Task<IEnumerable<string?>> GetHashValuesAsync(string hashKey, string[] keys)
    {
        RedisValue[] redisKeys = keys.Select(key => new RedisValue(key)).ToArray();
        var result = await _multiplexer.GetDatabase().HashGetAsync(hashKey, redisKeys);

        return result.Select(item =>
        {
            if (item.IsNull)
            {
                return null;
            }

            return item.ToString();
        });
    }
}
