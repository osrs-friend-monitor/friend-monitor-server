using OSRSFriendMonitor.Shared.Services.Database;
using OSRSFriendMonitor.Shared.Services.Database.Models;

namespace OSRSFriendMonitor.Shared.Services.Account;

public interface IAccountStorageService {
    public Task<UserAccount?> GetUserAccountAsync(string id);
    public Task<RunescapeAccount?> GetRunescapeAccountAsync(string accountHash);
    public Task<IDictionary<string, RunescapeAccount>> GetRunescapeAccountsAsync(IList<string> accountHashes);
    public Task CreateUserAccountAsync(UserAccount newAccount);
    public Task CreateOrUpdateRunescapeAccountAsync(RunescapeAccount account);
}

public class AccountStorageService: IAccountStorageService {
    private readonly IDatabaseService _databaseService;
    private readonly IAccountCache _cache;

    public AccountStorageService(IAccountCache cache, IDatabaseService databaseService)
    {
        _cache = cache;
        _databaseService = databaseService;
    }

    public async Task<UserAccount?> GetUserAccountAsync(string id) 
    {
        var account = await _cache.GetAccountAsync(id);

        if (account is not null)
        {
            return account;
        }

        var fromDatabase = await _databaseService.GetUserAccountAsync(id);

        if (fromDatabase is not null)
        {
            _cache.AddAccount(fromDatabase);
        }

        return fromDatabase;
    }

    public async Task<RunescapeAccount?> GetRunescapeAccountAsync(string accountHash)
    {
        var account = await _cache.GetRunescapeAccountAsync(accountHash);

        if (account is not null)
        {
            return account;
        }

        var fromDatabase = await _databaseService.GetRunescapeAccountAsync(accountHash);

        if (fromDatabase is not null)
        {
            _cache.AddRunescapeAccount(fromDatabase);
        }

        return fromDatabase;
    }

    public async Task<IDictionary<string, RunescapeAccount>> GetRunescapeAccountsAsync(IList<string> accountHashes)
    {
        (IDictionary<string, RunescapeAccount> results, 
         IList<string> idsMissingFromCache) = await _cache.GetRunescapeAccountsAsync(accountHashes);

        IDictionary<string, RunescapeAccount> accountsFromDatabase = await _databaseService.GetRunescapeAccountsAsync(idsMissingFromCache);

        foreach (var pair in accountsFromDatabase)
        {
            _cache.AddRunescapeAccount(pair.Value);
            results[pair.Key] = pair.Value;
        }

        return results;
    }

    public async Task CreateUserAccountAsync(UserAccount newAccount) 
    {
        UserAccount account = await _databaseService.CreateAccountAsync(newAccount);
        _cache.AddAccount(account);
    }

    public async Task CreateOrUpdateRunescapeAccountAsync(RunescapeAccount account) 
    {
        // TODO
        // RunescapeAccount newOrUpdatedAccount = await _databaseService.CreateOrUpdateRunescapeAccountAsync(account);
        // _cache.AddRunescapeAccount(newOrUpdatedAccount);
    }
}
