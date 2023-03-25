using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel;

namespace OSRSFriendMonitor.Shared.Services.Cache;

public interface ILocalCache
{
    public void SetItem(object key, object item, TimeSpan? expiration = null);
    public T? GetItem<T>(object key) where T : class;
}

public class LocalCache: ILocalCache
{
    private readonly IMemoryCache _cache;

    public LocalCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void SetItem(object key, object item, TimeSpan? expiration = null)
    {
        MemoryCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = expiration,
        };

        _cache.Set(key, item, options);
    }

    public T? GetItem<T>(object key) where T: class
    {
        if (!_cache.TryGetValue(key, out var item))
        {
            return null;
        }

        if (item is T t)
        {
            return t;
        } 
        else
        {
            return null;
        }
    }
}