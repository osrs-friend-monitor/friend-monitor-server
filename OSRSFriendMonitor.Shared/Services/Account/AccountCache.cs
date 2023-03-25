using System.Text.Json;
using Microsoft.Extensions.Logging;
using OSRSFriendMonitor.Shared.Services.Cache;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using StackExchange.Redis;

namespace OSRSFriendMonitor.Shared.Services.Account;

enum AccountCacheDataType
{
    Account = 0,
    ValidatedFriendsList = 1
}

record struct AccountCacheKey(
    long AccountHash,
    AccountCacheDataType DataType
)
{
    public override int GetHashCode()
    {
        return HashCode.Combine(AccountHash, DataType);
    }

    public override string ToString()
    {
        string prefix = DataType switch
        {
            AccountCacheDataType.Account => "account-",
            AccountCacheDataType.ValidatedFriendsList => "validated-",
            _ => throw new NotImplementedException(),
        };
        
        return prefix + AccountHash.ToString();
    }
}

public interface IAccountCache
{
    public Task<UserAccount?> GetAccountAsync(string userId);
    public Task<RunescapeAccount?> GetRunescapeAccountAsync(long accountHash);
    public Task<ValidatedFriendsList?> GetValidatedFriendsListAsync(long accountHash);
    public void AddAccount(UserAccount account);
    public void AddRunescapeAccount(RunescapeAccount account);
    public void AddValidatedFriendsList(long accountHash, long[] friendsList);
    public Task<(IDictionary<long, RunescapeAccount>, IList<long>)> GetRunescapeAccountsAsync(IEnumerable<long> accountHashes);
    public Task<(IDictionary<long, long[]>, IList<long>)> GetManyValidatedFriendsAsync(IEnumerable<long> accountHashes);
}

public class AccountCache : IAccountCache
{
    private readonly IRemoteCache _remote;
    private readonly ILocalCache _local;
    private readonly ILogger<AccountCache> _logger;
    private readonly Random _random;
    public AccountCache(IRemoteCache remote, ILocalCache local, ILogger<AccountCache> logger)
    {
        _random = new();
        _remote = remote;
        _local = local;
        _logger = logger;
    }

    public void AddAccount(UserAccount account)
    {
        _local.SetItem(account.Id, account, TimeSpan.FromSeconds(_random.Next(20, 45)));
        string json = JsonSerializer.Serialize(account, DatabaseModelJsonContext.Default.UserAccount);
        _remote.SetValueWithoutWaiting(new(account.Id, json), TimeSpan.FromHours(_random.Next(1, 3)));
    }

    public void AddRunescapeAccount(RunescapeAccount account)
    {
        _local.SetItem(account.AccountHash, account, RunescapeAccountLocalTimeSpan());
        string json = JsonSerializer.Serialize(account, DatabaseModelJsonContext.Default.RunescapeAccount);
        _remote.SetValueWithoutWaiting(new(account.Id, json), RunescapeAccountRemoteTimeSpan());
    }

    public void AddValidatedFriendsList(long accountHash, long[] friendsList)
    {
        AccountCacheKey key = ValidatedFriendsListKey(accountHash);
        _local.SetItem(key, friendsList, RunescapeAccountLocalTimeSpan());
        string json = JsonSerializer.Serialize(friendsList);
        _remote.SetValueWithoutWaiting(new(key.ToString(), json), RunescapeAccountRemoteTimeSpan());
    }

    public async Task<RunescapeAccount?> GetRunescapeAccountAsync(long accountHash)
    {
        AccountCacheKey key = new(accountHash, AccountCacheDataType.Account);
        RunescapeAccount? fromLocalCache = _local.GetItem<RunescapeAccount>(key);

        if (fromLocalCache is not null)
        {
            return fromLocalCache;
        }

        string? result = await _remote.GetValueAsync(key.ToString());

        if (result == null)
        {
            return null;
        }

        RunescapeAccount? account = JsonSerializer.Deserialize(result, DatabaseModelJsonContext.Default.RunescapeAccount);

        if (account is not null)
        {
            _local.SetItem(accountHash, account, RunescapeAccountLocalTimeSpan());
        }

        return account;
    }

    public async Task<UserAccount?> GetAccountAsync(string userId)
    {
        if (_local.GetItem<UserAccount>(userId) is UserAccount fromLocalCache)
        {
            return fromLocalCache;
        }

        string? result = await _remote.GetValueAsync(userId);

        if (result == null)
        {
            return null;
        }

        UserAccount? account = JsonSerializer.Deserialize(result, DatabaseModelJsonContext.Default.UserAccount);

        if (account is not null)
        {
            _local.SetItem(userId, account, AccountLocalTimeSpan());
        }

        return account;
    }

    public async Task<ValidatedFriendsList?> GetValidatedFriendsListAsync(long accountHash)
    {
        AccountCacheKey key = ValidatedFriendsListKey(accountHash);

        if (_local.GetItem<ValidatedFriendsList>(key) is ValidatedFriendsList fromLocalCache)
        {
            return fromLocalCache;
        }

        string? result = await _remote.GetValueAsync(key.ToString());

        if (result == null)
        {
            return null;
        }

        ValidatedFriendsList? friendsList = JsonSerializer.Deserialize(result, DatabaseModelJsonContext.Default.ValidatedFriendsList);

        if (friendsList is not null)
        {
            _local.SetItem(key, friendsList, RunescapeAccountLocalTimeSpan());
        }

        return friendsList;
    }

    public async Task<(IDictionary<long, RunescapeAccount>, IList<long>)> GetRunescapeAccountsAsync(IEnumerable<long> accountHashes)
    {

        IDictionary<long, RunescapeAccount> result = new Dictionary<long, RunescapeAccount>();

        IList<long> missingAccountHashesFromLocalCache = new List<long>();

        foreach (long accountHash in accountHashes)
        {
            RunescapeAccount? account = _local.GetItem<RunescapeAccount>(accountHash);

            if (account is null)
            {
                missingAccountHashesFromLocalCache.Add(accountHash);
            }
            else
            {
                result[account.AccountHash] = account;
            }
        }

        RedisValue[] cacheResults = await _remote.GetMultipleValuesAsync(missingAccountHashesFromLocalCache.Select(x => x.ToString()));

        IList<long> accountHashesWithMissingValues = new List<long>();

        for (int index = 0; index < cacheResults.Length; index++)
        {
            RedisValue cacheResult = cacheResults[index];

            if (cacheResult.IsNull)
            {
                accountHashesWithMissingValues.Add(missingAccountHashesFromLocalCache[index]);
                continue;
            }

            RunescapeAccount? account = JsonSerializer.Deserialize(cacheResult!, DatabaseModelJsonContext.Default.RunescapeAccount);

            if (account is null)
            {
                accountHashesWithMissingValues.Add(missingAccountHashesFromLocalCache[index]);
                _logger.LogError("Unable to parse runescape account from cache, {json}", cacheResult);
            }
            else
            {
                _local.SetItem(missingAccountHashesFromLocalCache[index], account, RunescapeAccountLocalTimeSpan());
                result[account.AccountHash] = account;
            }
        }

        return (result, accountHashesWithMissingValues);
    }

    public async Task<(IDictionary<long, long[]>, IList<long>)> GetManyValidatedFriendsAsync(IEnumerable<long> accountHashes)
    {
        IDictionary<long, long[]> result = new Dictionary<long, long[]>();

        IList<long> missingAccountHashesFromLocalCache = new List<long>();
        IList<AccountCacheKey> missingKeysFromLocalCache = new List<AccountCacheKey>();
        foreach (long accountHash in accountHashes)
        {
            AccountCacheKey cacheKey = ValidatedFriendsListKey(accountHash);
            long[]? friendsList = _local.GetItem<long[]>(cacheKey);

            if (friendsList is null)
            {
                missingAccountHashesFromLocalCache.Add(accountHash);
                missingKeysFromLocalCache.Add(cacheKey);
            }
            else
            {
                result[accountHash] = friendsList;
            }
        }

        RedisValue[] cacheResults = await _remote.GetMultipleValuesAsync(missingKeysFromLocalCache.Select(x => x.ToString()));

        IList<long> accountHashesWithMissingValues = new List<long>();

        for (int index = 0; index < cacheResults.Length; index++)
        {
            RedisValue cacheResult = cacheResults[index];

            if (cacheResult.IsNull)
            {
                accountHashesWithMissingValues.Add(missingAccountHashesFromLocalCache[index]);
                continue;
            }

            long[]? friendsListFromRemoteCache = JsonSerializer.Deserialize<long[]>(cacheResult!);

            if (friendsListFromRemoteCache is null)
            {
                accountHashesWithMissingValues.Add(missingAccountHashesFromLocalCache[index]);
                _logger.LogError("Unable to parse runescape account from cache, {json}", cacheResult);
            }
            else
            {
                _local.SetItem(missingKeysFromLocalCache[index], friendsListFromRemoteCache, RunescapeAccountLocalTimeSpan());
                result[missingAccountHashesFromLocalCache[index]] = friendsListFromRemoteCache;
            }
        }

        return (result, accountHashesWithMissingValues);
    }

    private static AccountCacheKey ValidatedFriendsListKey(long accountHash)
    {
        return new AccountCacheKey(accountHash, AccountCacheDataType.ValidatedFriendsList);
    }

    private TimeSpan AccountLocalTimeSpan()
    {
        return TimeSpan.FromMinutes(_random.Next(60, 180));
    }

    private TimeSpan AccountRemoteTimeSpan()
    {
        return TimeSpan.FromMinutes(_random.Next(300, 420));
    }

    private TimeSpan RunescapeAccountLocalTimeSpan()
    {
        return TimeSpan.FromMinutes(_random.Next(10, 30));
    }

    private TimeSpan RunescapeAccountRemoteTimeSpan()
    {
        return TimeSpan.FromMinutes(_random.Next(30, 75));
    }
}
