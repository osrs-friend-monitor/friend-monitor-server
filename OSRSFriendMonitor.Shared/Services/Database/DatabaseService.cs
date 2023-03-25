using Microsoft.Azure.Cosmos;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Collections.Immutable;
using System.Security.Principal;

namespace OSRSFriendMonitor.Shared.Services.Database;

public interface IDatabaseService
{
    Task<(RunescapeAccount, string)?> GetRunescapeAccountWithEtagAsync(long accountHash);
    Task<RunescapeAccount?> GetRunescapeAccountAsync(long accountHash);

    Task<ValidatedFriendsList?> GetValidatedFriendsListAsync(long accountHash);
    Task<IDictionary<long, ValidatedFriendsList>> GetValidatedFriendsListsAsync(IEnumerable<long> accountHashes);
    Task<RunescapeAccount> UpdateRunescapeAccountAsync(
        long accountHash,
        string displayName
    );
    Task<IDictionary<long, RunescapeAccount>> GetRunescapeAccountsAsync(IEnumerable<long> accountHashes);
    Task<ActivityUpdate> InsertActivityUpdateAsync(ActivityUpdate update);
    Task<RunescapeAccount> CreateOrUpdateRunescapeAccountAsync(RunescapeAccount account, string? etag);

    Task<InGameFriendsList?> GetInGameFriendsListAsync(string displayName);
    Task<IDictionary<string, InGameFriendsList>> GetInGameFriendsListsAsync(IEnumerable<string> displayNames);

    Task UpdateInGameFriendsListAsync(InGameFriendsList friendsList);
    Task DeleteInGameFriendsListAsync(string displayName, long accountHash);

    Task<(ValidatedFriendsList, string)?> GetValidatedFriendsListWithEtagAsync(long accountHash);
    Task<ValidatedFriendsList> UpdateValidatedFriendsListAsync(ValidatedFriendsList friendsList, string? etag);
}

public class DatabaseService : IDatabaseService
{
    private readonly Container _accountsContainer;
    private readonly Container _activityContainer;
    private readonly Container _inGameFriendsListContainer;
    private readonly Container _validatedFriendsListContainer;

    public DatabaseService(
        Container accountsContainer, 
        Container activityContainer, 
        Container inGameFriendsListContainer,
        Container validatedFriendsListContainer
    )
    {
        _accountsContainer = accountsContainer;
        _activityContainer = activityContainer;
        _inGameFriendsListContainer = inGameFriendsListContainer;
        _validatedFriendsListContainer = validatedFriendsListContainer;
     }

    async Task<ActivityUpdate> IDatabaseService.InsertActivityUpdateAsync(ActivityUpdate update) {
        return await _activityContainer.CreateItemAsync(update, new(update.PartitionKey));
    }

    public async Task<RunescapeAccount> UpdateRunescapeAccountAsync(
        long accountHash, 
        string displayName
    )
    {
        string displayNamePath = RunescapeAccount.DisplayNamePath();

        PatchOperation displayNameOperation = PatchOperation.Set(displayNamePath, displayName);

        return await _accountsContainer.PatchItemAsync<RunescapeAccount>(
            accountHash.ToString(),
            new(accountHash),
            new[] { displayNameOperation }
        );
    }

    async Task<RunescapeAccount> IDatabaseService.CreateOrUpdateRunescapeAccountAsync(RunescapeAccount account, string? etag)
    {
        if (etag is null)
        {
            return await _accountsContainer.CreateItemAsync(account, new(account.Id));
        }
        else
        {
            return await _accountsContainer.ReplaceItemAsync(
                account,
                account.Id,
                new(account.Id),
                new ItemRequestOptions { IfMatchEtag = etag }
            );
        }
    }

    public async Task<RunescapeAccount?> GetRunescapeAccountAsync(long accountHash)
    {
        var result = await GetRunescapeAccountWithEtagAsync(accountHash);

        if (result is null)
        {
            return null;
        }
        else
        {
            return result.GetValueOrDefault().Item1;
        }
    }

    public async Task<ValidatedFriendsList?> GetValidatedFriendsListAsync(long accountHash)
    {
        (ValidatedFriendsList, string)? result = await GetValidatedFriendsListWithEtagAsync(accountHash);

        if (result is null)
        {
            return null;
        }
        else
        {
            return result.GetValueOrDefault().Item1;
        }
    }
    public async Task<(RunescapeAccount, string)?> GetRunescapeAccountWithEtagAsync(long accountHash)
    {
        try
        {
            string accountHashAsString = accountHash.ToString();
            ItemResponse<RunescapeAccount> response = await _accountsContainer.ReadItemAsync<RunescapeAccount>(accountHashAsString, new(accountHashAsString));

            if (response.Resource is null) 
            {
                return null;
            }

            return (response.Resource, response.ETag);
        }
        catch (Exception)
        {
            return null;
        }
    }

    async Task<IDictionary<long, ValidatedFriendsList>> IDatabaseService.GetValidatedFriendsListsAsync(IEnumerable<long> accountHashes) 
    {
        IReadOnlyList<(string, PartitionKey)> queryItems = accountHashes.Select<long, (string, PartitionKey)>(accountHash =>
        {
            return (accountHash.ToString(), new(accountHash));
        }).ToList();

        IDictionary<long, ValidatedFriendsList> results = new Dictionary<long, ValidatedFriendsList>(capacity: queryItems.Count);

        if (queryItems.Count == 0)
        {
            return results;
        }

        try
        {
            FeedResponse<ValidatedFriendsList> feed = await _accountsContainer.ReadManyItemsAsync<ValidatedFriendsList>(queryItems);

            foreach (var friendsList in feed)
            {
                results[friendsList.AccountHash] = friendsList;
            }
        }
        catch (Exception)
        {

        }

        return results;
    }

    async Task<IDictionary<string, InGameFriendsList>> IDatabaseService.GetInGameFriendsListsAsync(IEnumerable<string> displayNames)
    {
        IReadOnlyList<(string, PartitionKey)> queryItems = displayNames.Select<string, (string, PartitionKey)>(accountHash =>
        {
            return (accountHash, new(accountHash));
        }).ToList();

        IDictionary<string, InGameFriendsList> results = new Dictionary<string, InGameFriendsList>(capacity: queryItems.Count);

        if (queryItems.Count == 0)
        {
            return results;
        }

        try
        {
            FeedResponse<InGameFriendsList> feed = await _accountsContainer.ReadManyItemsAsync<InGameFriendsList>(queryItems);

            foreach (var friendsList in feed)
            {
                results[friendsList.DisplayName] = friendsList;
            }
        }
        catch (Exception)
        {

        }

        return results;
    }

    async Task<IDictionary<long, RunescapeAccount>> IDatabaseService.GetRunescapeAccountsAsync(IEnumerable<long> accountHashes)
    {

        IReadOnlyList<(string, PartitionKey)> queryItems = accountHashes.Select<long, (string, PartitionKey)>(accountHash =>
        {
            return (accountHash.ToString(), new(accountHash));
        }).ToList();

        IDictionary<long, RunescapeAccount> results = new Dictionary<long, RunescapeAccount>(capacity: queryItems.Count);

        if (queryItems.Count == 0)
        {
            return results;
        }

        try
        {
            FeedResponse<RunescapeAccount> feed = await _accountsContainer.ReadManyItemsAsync<RunescapeAccount>(queryItems);

            foreach (var account in feed)
            {
                results[account.AccountHash] = account;
            }
        }
        catch (Exception)
        {

        }

        return results;
    }

    public async Task<InGameFriendsList?> GetInGameFriendsListAsync(string displayName)
    {
        try
        {
            return await _inGameFriendsListContainer.ReadItemAsync<InGameFriendsList>(displayName, new(displayName));
        } catch (Exception) {
            return null;
        }
    }

    async Task IDatabaseService.DeleteInGameFriendsListAsync(string displayName, long accountHash)
    {
        try
        {
            InGameFriendsList? friendsList = await GetInGameFriendsListAsync(displayName);

            if (friendsList is null)
            {
                return;
            }

            if (friendsList.AccountHash != accountHash)
            {
                return;
            }

            await _inGameFriendsListContainer.DeleteItemAsync<InGameFriendsList>(
                displayName, 
                new(displayName), 
                new ItemRequestOptions { EnableContentResponseOnWrite = false }
            );
        } catch (Exception) { }
    }

    async Task IDatabaseService.UpdateInGameFriendsListAsync(InGameFriendsList friendsList)
    {
        try
        {
            await _inGameFriendsListContainer.UpsertItemAsync(
                friendsList, 
                new(friendsList.DisplayName),
                new ItemRequestOptions { EnableContentResponseOnWrite = false}
            );
        } catch (Exception) { }
    }

    public async Task<(ValidatedFriendsList, string)?> GetValidatedFriendsListWithEtagAsync(long accountHash)
    {
        try
        {
            ItemResponse<ValidatedFriendsList> response = await _validatedFriendsListContainer.ReadItemAsync<ValidatedFriendsList>(
                accountHash.ToString(), 
                new(accountHash)
            );

            if (response.Resource is null)
            {
                return null;
            }

            return (response.Resource, response.ETag);
        }
        catch (Exception)
        {
            return null;
        }
    }

    async Task<ValidatedFriendsList> IDatabaseService.UpdateValidatedFriendsListAsync(ValidatedFriendsList friendsList, string? etag)
    {
        if (etag is null)
        {
            return await _validatedFriendsListContainer.CreateItemAsync(friendsList, new(friendsList.AccountHash));
        }
        else
        {
            return await _validatedFriendsListContainer.ReplaceItemAsync(
                friendsList,
                friendsList.AccountHash.ToString(),
                new(friendsList.AccountHash),
                new ItemRequestOptions { IfMatchEtag = etag }
            );
        }
    }
}
