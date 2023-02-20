using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using OSRSFriendMonitor.Shared.Services.Cache;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using StackExchange.Redis;

namespace OSRSFriendMonitor.Shared.Services.Account;

public interface IAccountCache
{
    public Task<UserAccount?> GetAccountAsync(string userId);
    public Task<RunescapeAccount?> GetRunescapeAccountAsync(string accountHash);
    public Task<ValidatedFriendsList?> GetValidatedFriendsListAsync(string accountHash);
    public void AddAccount(UserAccount account);
    public void AddRunescapeAccount(RunescapeAccount account);
    public void AddValidatedFriendsList(string accountHash, string[] friendsList);
    public Task<(IDictionary<string, RunescapeAccount>, IList<string>)> GetRunescapeAccountsAsync(IList<string> accountHashes);
    public Task<(IDictionary<string, string[]>, IList<string>)> GetManyValidatedFriendsAsync(IList<string> accountHashes);
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
        _remote.SetValueWithoutWaiting(new(account.AccountHash, json), RunescapeAccountRemoteTimeSpan());
    }

    public void AddValidatedFriendsList(string accountHash, string[] friendsList)
    {
        string key = ValidatedFriendsListKey(accountHash);
        _local.SetItem(key, friendsList, RunescapeAccountLocalTimeSpan());
        string json = JsonSerializer.Serialize(friendsList);
        _remote.SetValueWithoutWaiting(new(key, json), RunescapeAccountRemoteTimeSpan());
    }

    public async Task<RunescapeAccount?> GetRunescapeAccountAsync(string accountHash)
    {
        RunescapeAccount? fromLocalCache = _local.GetItem<RunescapeAccount>(accountHash);

        if (fromLocalCache is not null)
        {
            return fromLocalCache;
        }

        string? result = await _remote.GetValueAsync(accountHash);

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

    public async Task<ValidatedFriendsList?> GetValidatedFriendsListAsync(string accountHash)
    {
        string key = ValidatedFriendsListKey(accountHash);

        if (_local.GetItem<ValidatedFriendsList>(key) is ValidatedFriendsList fromLocalCache)
        {
            return fromLocalCache;
        }

        string? result = await _remote.GetValueAsync(key);

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

    public async Task<(IDictionary<string, RunescapeAccount>, IList<string>)> GetRunescapeAccountsAsync(IList<string> accountHashes)
    {

        IDictionary<string, RunescapeAccount> result = new Dictionary<string, RunescapeAccount>();

        IList<string> missingAccountHashesFromLocalCache = new List<string>();

        foreach (string accountHash in accountHashes)
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

        RedisValue[] cacheResults = await _remote.GetMultipleValuesAsync(missingAccountHashesFromLocalCache);

        IList<string> accountHashesWithMissingValues = new List<string>();

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

    public async Task<(IDictionary<string, string[]>, IList<string>)> GetManyValidatedFriendsAsync(IList<string> accountHashes)
    {
        IDictionary<string, string[]> result = new Dictionary<string, string[]>();

        IList<string> missingAccountHashesFromLocalCache = new List<string>();
        IList<string> missingKeysFromLocalCache = new List<string>();
        foreach (string accountHash in accountHashes)
        {
            string cacheKey = ValidatedFriendsListKey(accountHash);
            string[]? friendsList = _local.GetItem<string[]>(cacheKey);

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

        RedisValue[] cacheResults = await _remote.GetMultipleValuesAsync(missingKeysFromLocalCache);

        IList<string> accountHashesWithMissingValues = new List<string>();

        for (int index = 0; index < cacheResults.Length; index++)
        {
            RedisValue cacheResult = cacheResults[index];

            if (cacheResult.IsNull)
            {
                accountHashesWithMissingValues.Add(missingAccountHashesFromLocalCache[index]);
                continue;
            }

            string[]? friendsListFromRemoteCache = JsonSerializer.Deserialize<string[]>(cacheResult!);

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

    private static string ValidatedFriendsListKey(string accountHash)
    {
        return $"validated-friends-list-{accountHash}";
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
        return TimeSpan.FromSeconds(_random.Next(20, 45));
    }

    private TimeSpan RunescapeAccountRemoteTimeSpan()
    {
        return TimeSpan.FromMinutes(_random.Next(30, 60));
    }
}
