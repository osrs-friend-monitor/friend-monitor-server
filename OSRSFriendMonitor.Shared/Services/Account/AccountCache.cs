using System.Text.Json;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OSRSFriendMonitor.Shared.Services.Cache;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using StackExchange.Redis;

namespace OSRSFriendMonitor.Shared.Services.Account;

public interface IAccountCache
{
    public Task<UserAccount?> GetAccountAsync(string userId);
    public Task<RunescapeAccount?> GetRunescapeAccountAsync(RunescapeAccountIdentifier id);
    public void AddAccount(UserAccount account);
    public void AddRunescapeAccount(RunescapeAccount account);
    public Task<(IDictionary<RunescapeAccountIdentifier, RunescapeAccount>, IList<RunescapeAccountIdentifier>)> GetRunescapeAccountsAsync(IList<RunescapeAccountIdentifier> ids);
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
        string id = account.AccountIdentifier.CombinedIdentifier();
        _local.SetItem(id, account, RunescapeAccountLocalTimeSpan());
        string json = JsonSerializer.Serialize(account, DatabaseModelJsonContext.Default.RunescapeAccount);
        _remote.SetValueWithoutWaiting(new(id, json), RunescapeAccountRemoteTimeSpan());
    }

    public async Task<RunescapeAccount?> GetRunescapeAccountAsync(RunescapeAccountIdentifier id)
    {
        string key = id.CombinedIdentifier();
        RunescapeAccount? fromLocalCache = _local.GetItem<RunescapeAccount>(key);

        if (fromLocalCache is not null)
        {
            return fromLocalCache;
        }

        string? result = await _remote.GetValueAsync(key);

        if (result == null)
        {
            return null;
        }

        RunescapeAccount? account = JsonSerializer.Deserialize(result, DatabaseModelJsonContext.Default.RunescapeAccount);

        if (account is not null)
        {
            _local.SetItem(key, account, RunescapeAccountLocalTimeSpan());
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

    public async Task<(IDictionary<RunescapeAccountIdentifier, RunescapeAccount>, IList<RunescapeAccountIdentifier>)> GetRunescapeAccountsAsync(IList<RunescapeAccountIdentifier> ids)
    {
        IList<string> idsAsStrings = ids.Select(id => id.CombinedIdentifier()).ToList();

        IDictionary<RunescapeAccountIdentifier, RunescapeAccount> result = new Dictionary<RunescapeAccountIdentifier, RunescapeAccount>();

        IList<string> missingIdsFromLocalCache = new List<string>();

        foreach (string id in idsAsStrings)
        {
            RunescapeAccount? account = _local.GetItem<RunescapeAccount>(id);

            if (account is null)
            {
                missingIdsFromLocalCache.Add(id);
            }
            else
            {
                result[account.AccountIdentifier] = account;
            }
        }

        RedisValue[] cacheResults = await _remote.GetMultipleValuesAsync(missingIdsFromLocalCache);

        IList<RunescapeAccountIdentifier> idsWithMissingValues = new List<RunescapeAccountIdentifier>();

        for (int index = 0; index < cacheResults.Length; index++)
        {
            RedisValue cacheResult = cacheResults[index];

            if (cacheResult.IsNull)
            {
                idsWithMissingValues.Add(RunescapeAccountIdentifier.FromString(missingIdsFromLocalCache[index]));
                continue;
            }

            RunescapeAccount? account = JsonSerializer.Deserialize(cacheResult!, DatabaseModelJsonContext.Default.RunescapeAccount);

            if (account is null)
            {
                idsWithMissingValues.Add(RunescapeAccountIdentifier.FromString(missingIdsFromLocalCache[index]));
                _logger.LogError("Unable to parse runescape account from cache, {json}", cacheResult);
            }
            else
            {
                _local.SetItem(missingIdsFromLocalCache[index], account, RunescapeAccountLocalTimeSpan());
                result[account.AccountIdentifier] = account;
            }
        }

        return (result, idsWithMissingValues);
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
