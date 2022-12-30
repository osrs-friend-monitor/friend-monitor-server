using OSRSFriendMonitor.Shared.Services.Database;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Collections.Immutable;

namespace OSRSFriendMonitor.Shared.Services.Account;

public interface IAccountStorageService {
    public Task<RunescapeAccount?> GetRunescapeAccountAsync(string accountHash);
    public Task<IDictionary<string, RunescapeAccount>> GetRunescapeAccountsAsync(IList<string> accountHashes);
    public Task<RunescapeAccount?> CreateRunescapeAccountOrUpdateNameAsync(
        string accountHash,
        string displayName,
        string userId,
        string? previousDisplayName
    );
}

public class AccountStorageService: IAccountStorageService {
    private readonly IDatabaseService _databaseService;
    private readonly IAccountCache _cache;

    public AccountStorageService(IAccountCache cache, IDatabaseService databaseService)
    {
        _cache = cache;
        _databaseService = databaseService;
    }

    public async Task<RunescapeAccount?> GetRunescapeAccountAsync(string accountHash)
    {
        var account = await _cache.GetRunescapeAccountAsync(accountHash);

        if (account is not null)
        {
            return account;
        }

        RunescapeAccount? fromDatabase = await _databaseService.GetRunescapeAccountAsync(accountHash);

        if (fromDatabase is not null)
        {
            _cache.AddRunescapeAccount(fromDatabase);
        }

        return fromDatabase;
    }

    public async Task<RunescapeAccount?> CreateRunescapeAccountOrUpdateNameAsync(
        string accountHash, 
        string displayName, 
        string userId,
        string? previousDisplayName
    )
    {
        RunescapeAccount? account = await GetRunescapeAccountAsync(accountHash);
        
        if (account is null)
        {
            RunescapeAccount newAccount = new(
                AccountHash: accountHash,
                UserId: userId,
                DisplayName: displayName,
                PreviousName: previousDisplayName,
                Friends: ImmutableList<Friend>.Empty
            );

            RunescapeAccount newAccountInDatabase = await _databaseService.CreateOrUpdateRunescapeAccountAsync(newAccount, null);

            _cache.AddRunescapeAccount(newAccountInDatabase);
            return newAccount;
        }
        else if (account.UserId != userId)
        {
            throw new InvalidOperationException($"User ID {userId} does not match account's user ID {account.UserId}");
        }
        else if (account.DisplayName != displayName || account.PreviousName != previousDisplayName)
        {
            RunescapeAccount updatedAccount = await _databaseService.UpdateRunescapeAccountDisplayNameAsync(
                accountHash: accountHash,
                displayName: displayName,
                previousDisplayName: previousDisplayName
            );

            _cache.AddRunescapeAccount(updatedAccount);

            return updatedAccount;
        }
        else
        {
            return account;
        }
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
}
