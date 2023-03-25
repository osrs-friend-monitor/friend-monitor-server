using OSRSFriendMonitor.Shared.Services.Account.Queue;
using OSRSFriendMonitor.Shared.Services.Account.Queue.Messages;
using OSRSFriendMonitor.Shared.Services.Database;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Collections.Immutable;

namespace OSRSFriendMonitor.Shared.Services.Account;

public interface IAccountStorageService {
    public Task<RunescapeAccount?> GetRunescapeAccountAsync(long accountHash);
    public Task<IDictionary<long, RunescapeAccount>> GetRunescapeAccountsAsync(IEnumerable<long> accountHashes);
    public Task<RunescapeAccount?> CreateRunescapeAccountOrUpdateAsync(
        long accountHash,
        string displayName,
        string userId,
        string[]? friends
    );

    public Task<ValidatedFriendsList?> GetValidatedFriendsListForAccountAsync(long accountHash);
    public Task<IDictionary<long, long[]>> GetValidatedFriendsListsForAccountsAsync(IEnumerable<long> accountHashes);
}

public record RunescapeAccountFriendUpdateRequest(
    string AccountHash
);

public class AccountStorageService: IAccountStorageService {
    private readonly IDatabaseService _databaseService;
    private readonly IAccountCache _cache;
    private readonly IQueueWriter<ValidatedFriendsListUpdateRequest> _queueWriter;

    public AccountStorageService(IAccountCache cache, IDatabaseService databaseService, IQueueWriter<ValidatedFriendsListUpdateRequest> queueWriter)
    {
        _cache = cache;
        _databaseService = databaseService;
        _queueWriter = queueWriter;
    }

    public async Task<RunescapeAccount?> GetRunescapeAccountAsync(long accountHash)
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

    public async Task<ValidatedFriendsList?> GetValidatedFriendsListForAccountAsync(long accountHash)
    {
        var friendsList = await _cache.GetValidatedFriendsListAsync(accountHash);

        if (friendsList is not null)
        {
            return friendsList;
        }

        ValidatedFriendsList? fromDatabase = await _databaseService.GetValidatedFriendsListAsync(accountHash);

        if (fromDatabase is not null)
        {
            long[] friendsAsArray = fromDatabase.Friends
                .Where(friend => friend.AccountHash is not null)
                .Select(friend => friend.AccountHash!)
                .Cast<long>()
                .ToArray();

            _cache.AddValidatedFriendsList(accountHash, friendsAsArray);
        }

        return fromDatabase;
    }

    public async Task<RunescapeAccount?> CreateRunescapeAccountOrUpdateAsync(
        long accountHash, 
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

        // TODO: delete old in game friends list if name has changed? maybe? or rely on ttl
        if (account.DisplayName != displayName)
        {
            await _databaseService.DeleteInGameFriendsListAsync(account.DisplayName, accountHash);

            account = await _databaseService.UpdateRunescapeAccountAsync(accountHash, displayName);

            await CreateOrUpdateInGameFriendsListAsync(
                displayName: displayName,
                accountHash: accountHash,
                friends?.ToImmutableHashSet() ?? ImmutableHashSet<string>.Empty
            );
        }

        // TODO: maybe replace anyway if equal (sometimes) because ttl
        if (friends is not null) 
        {
            var friendsList = await _databaseService.GetInGameFriendsListAsync(displayName);
            IImmutableSet<string> newFriendsSet = friends.ToImmutableHashSet();

            if (friendsList is null || !friendsList.FriendDisplayNames.Equals(newFriendsSet))
            {
                await CreateOrUpdateInGameFriendsListAsync(displayName: displayName, accountHash: accountHash, friends.ToImmutableHashSet());
            }

        }

        _cache.AddRunescapeAccount(account);

        return account;
    }

    private async Task CreateOrUpdateInGameFriendsListAsync(string displayName, long accountHash, IImmutableSet<string> friends)
    {
        await _databaseService.UpdateInGameFriendsListAsync(
            new(
                DisplayName: displayName,
                AccountHash: accountHash,
                FriendDisplayNames: friends
            )
        );

        await _queueWriter.EnqueueMessageAsync(new(accountHash, DateTime.UtcNow), QueueMessageJsonContext.Default.ValidatedFriendsListUpdateRequest);
    }

    public async Task<IDictionary<long, RunescapeAccount>> GetRunescapeAccountsAsync(IEnumerable<long> accountHashes)
    {
        (IDictionary<long, RunescapeAccount> results, 
         IList<long> idsMissingFromCache) = await _cache.GetRunescapeAccountsAsync(accountHashes);

        IDictionary<long, RunescapeAccount> accountsFromDatabase = await _databaseService.GetRunescapeAccountsAsync(idsMissingFromCache);

        foreach (var pair in accountsFromDatabase)
        {
            _cache.AddRunescapeAccount(pair.Value);
            results[pair.Key] = pair.Value;
        }

        return results;
    }

    public async Task<IDictionary<long, long[]>> GetValidatedFriendsListsForAccountsAsync(IEnumerable<long> accountHashes)
    {
        (IDictionary<long, long[]> results,
         IList<long> idsMissingFromCache) = await _cache.GetManyValidatedFriendsAsync(accountHashes);

        IDictionary<long, ValidatedFriendsList> accountsFromDatabase = await _databaseService.GetValidatedFriendsListsAsync(idsMissingFromCache);

        foreach (var pair in accountsFromDatabase)
        {
            long[] friendsListAsArray = pair.Value.Friends.Where(list => list.AccountHash is not null).Select(list => list.AccountHash!).Cast<long>().ToArray();
            _cache.AddValidatedFriendsList(pair.Key, friendsListAsArray);
            results[pair.Key] = friendsListAsArray;
        }

        return results;
    }
}
