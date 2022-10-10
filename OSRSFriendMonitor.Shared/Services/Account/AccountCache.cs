using System.Text.Json;
using Microsoft.Extensions.Logging;
using OSRSFriendMonitor.Shared.Services.Cache;
using OSRSFriendMonitor.Shared.Services.Database.Models;

namespace OSRSFriendMonitor.Shared.Services.Account;

public interface IAccountCache
{
    public Task<UserAccount?> GetAccountAsync(string userId);
    public Task<RunescapeAccount?> GetRunescapeAccountAsync(RunescapeAccountIdentifier id);
    public void AddAccount(UserAccount account);
    public void AddRunescapeAccount(RunescapeAccount account);
    public Task<IDictionary<RunescapeAccountIdentifier, RunescapeAccount>> GetRunescapeAccountsAsync(IList<RunescapeAccountIdentifier> ids);
}

public class AccountCache : IAccountCache
{
    private readonly IRemoteCache _cache;
    private readonly ILogger<AccountCache> _logger;

    public AccountCache(IRemoteCache cache, ILogger<AccountCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public void AddAccount(UserAccount account)
    {
        string json = JsonSerializer.Serialize(account, DatabaseModelJsonContext.Default.UserAccount);
        _cache.SetValueWithoutWaiting(new(account.Id, json), TimeSpan.FromMinutes(5));
    }

    public void AddRunescapeAccount(RunescapeAccount account)
    {
        string json = JsonSerializer.Serialize(account, DatabaseModelJsonContext.Default.RunescapeAccount);
        _cache.SetValueWithoutWaiting(new(account.AccountIdentifier.CombinedIdentifier(), json), TimeSpan.FromMinutes(5));
    }

    public async Task<RunescapeAccount?> GetRunescapeAccountAsync(RunescapeAccountIdentifier id)
    {
        string? result = await _cache.GetValueAsync(id.CombinedIdentifier());

        if (result == null)
        {
            return null;
        }

        RunescapeAccount? account = JsonSerializer.Deserialize(result, DatabaseModelJsonContext.Default.RunescapeAccount);

        return account;
    }

    public async Task<UserAccount?> GetAccountAsync(string userId)
    {
        string? result = await _cache.GetValueAsync(userId);

        if (result == null)
        {
            return null;
        }

        UserAccount? account = JsonSerializer.Deserialize(result, DatabaseModelJsonContext.Default.UserAccount);

        return account;
    }

    public async Task<IDictionary<RunescapeAccountIdentifier, RunescapeAccount>> GetRunescapeAccountsAsync(IList<RunescapeAccountIdentifier> ids)
    {
        IEnumerable<KeyValuePair<string, string>> cacheResults = await _cache.GetMultipleValuesAsync(ids.Select(x => x.CombinedIdentifier()));
        IDictionary<RunescapeAccountIdentifier, RunescapeAccount> result = new Dictionary<RunescapeAccountIdentifier, RunescapeAccount>();

        foreach (var pair in cacheResults)
        {
            RunescapeAccount? account = JsonSerializer.Deserialize(pair.Value, DatabaseModelJsonContext.Default.RunescapeAccount);

            if (account is null)
            {
                _logger.LogError("Unable to parse runescape account from cache, {json}", pair.Value);
            }
            else
            {
                result[account.AccountIdentifier] = account;
            }
        }
        return result;
    }
}
