using System.Text.Json;
using Microsoft.Extensions.Logging;
using OSRSFriendMonitor.Shared.Services.Cache;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using StackExchange.Redis;

namespace OSRSFriendMonitor.Shared.Services.Account;

public interface IAccountCache
{
    public Task<UserAccount?> GetAccountAsync(string userId);
    public Task<RunescapeAccount?> GetRunescapeAccountAsync(string accountHash);
    public void AddAccount(UserAccount account);
    public void AddRunescapeAccount(RunescapeAccount account);
    public Task<(IDictionary<string, RunescapeAccount>, IList<string>)> GetRunescapeAccountsAsync(IList<string> accountHashes);
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
