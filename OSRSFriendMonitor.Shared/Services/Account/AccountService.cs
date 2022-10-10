using OSRSFriendMonitor.Shared.Services.Database;
using OSRSFriendMonitor.Shared.Services.Database.Models;

namespace OSRSFriendMonitor.Shared.Services.Account;

public interface IAccountService {
    public Task<UserAccount?> GetUserAccountAsync(string id);
    public Task<RunescapeAccount?> GetRunescapeAccountAsync(RunescapeAccountIdentifier id);
    public Task CreateUserAccountAsync(UserAccount newAccount);
    public Task CreateOrUpdateRunescapeAccountDisplayNameAsync(RunescapeAccountIdentifier id, string displayName);
}

public class AccountService: IAccountService {
    private readonly IDatabaseService _databaseService;
    private readonly IAccountCache _cache;
    public AccountService(IAccountCache cache, IDatabaseService databaseService)
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

    public async Task<RunescapeAccount?> GetRunescapeAccountAsync(RunescapeAccountIdentifier id)
    {
        var account = await _cache.GetRunescapeAccountAsync(id);

        if (account is not null)
        {
            return account;
        }

        var fromDatabase = await _databaseService.GetRunescapeAccountAsync(id);

        if (fromDatabase is not null)
        {
            _cache.AddRunescapeAccount(fromDatabase);
        }

        return fromDatabase;
    }


    public async Task CreateUserAccountAsync(UserAccount newAccount) 
    {
        UserAccount account = await _databaseService.CreateAccountAsync(newAccount);
        _cache.AddAccount(account);
    }

    public async Task CreateOrUpdateRunescapeAccountDisplayNameAsync(RunescapeAccountIdentifier id, string displayName) 
    {
        RunescapeAccount account = await _databaseService.CreateOrUpdateRunescapeAccountDisplayNameAsync(id, displayName);
        _cache.AddRunescapeAccount(account);
    }
}
