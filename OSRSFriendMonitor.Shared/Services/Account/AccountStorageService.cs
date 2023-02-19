using OSRSFriendMonitor.Shared.Services.Database;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Collections.Immutable;

namespace OSRSFriendMonitor.Shared.Services.Account;

public interface IAccountStorageService {
    public Task<RunescapeAccount?> GetRunescapeAccountAsync(string accountHash);
    public Task<IDictionary<string, RunescapeAccount>> GetRunescapeAccountsAsync(IList<string> accountHashes);
    public Task<RunescapeAccount?> CreateRunescapeAccountOrUpdateAsync(
        string accountHash,
        string displayName,
        string userId,
        string[]? friends
    );

    public Task<ValidatedFriendsList?> GetValidatedFriendsListForAccountAsync(string accountHash);
}

public record RunescapeAccountFriendUpdateRequest(
    string AccountHash
);

public class AccountStorageService: IAccountStorageService {
    private readonly IDatabaseService _databaseService;
    private readonly IAccountCache _cache;

    private Action<RunescapeAccountFriendUpdateRequest> _accountFriendUpdateRequest;

    public AccountStorageService(IAccountCache cache, IDatabaseService databaseService, Action<RunescapeAccountFriendUpdateRequest> accountFriendUpdateRequest)
    {
        _cache = cache;
        _databaseService = databaseService;
        _accountFriendUpdateRequest = accountFriendUpdateRequest;
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

    public async Task<ValidatedFriendsList?> GetValidatedFriendsListForAccountAsync(string accountHash)
    {
        var friendsList = await _cache.GetValidatedFriendsListAsync(accountHash);

        if (friendsList is not null)
        {
            return friendsList;
        }

        ValidatedFriendsList? fromDatabase = await _databaseService.GetValidatedFriendsListAsync(accountHash);

        if (fromDatabase is not null)
        {
            _cache.AddValidatedFriendsList(fromDatabase);
        }

        return fromDatabase;
    }

    public async Task<RunescapeAccount?> CreateRunescapeAccountOrUpdateAsync(
        string accountHash, 
        string displayName, 
        string userId,
        string[]? friends
    )
    {
        RunescapeAccount? account = await GetRunescapeAccountAsync(accountHash);

        if (account is not null && account.UserId != userId)
        {
            throw new InvalidOperationException($"User ID {userId} does not match account's user ID {account.UserId}");
        }

        if (account is null)
        {
            RunescapeAccount newAccount = new(
                AccountHash: accountHash,
                UserId: userId,
                DisplayName: displayName
            );

            account = await _databaseService.CreateOrUpdateRunescapeAccountAsync(newAccount, null);
        } 

        if (account.DisplayName != displayName)
        {
            await _databaseService.DeleteInGameFriendsListAsync(account.DisplayName, accountHash);

            account = await _databaseService.UpdateRunescapeAccountAsync(accountHash, displayName);
        }

        if (friends is not null) 
        {
            var friendsList = await _databaseService.GetInGameFriendsListAsync(displayName);
            IImmutableSet<string> newFriendsSet = friends.ToImmutableHashSet();

            if (!(friendsList?.FriendDisplayNames.Equals(newFriendsSet) ?? false))
            {
                _accountFriendUpdateRequest(new(accountHash));
            }

        }

        _cache.AddRunescapeAccount(account);

        return account;
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
