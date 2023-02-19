using Microsoft.Azure.Cosmos;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Collections.Immutable;
using System.Security.Principal;

namespace OSRSFriendMonitor.Shared.Services.Database;

public interface IDatabaseService
{
    Task<(RunescapeAccount, string)?> GetRunescapeAccountWithEtagAsync(string accountHash);
    Task<RunescapeAccount?> GetRunescapeAccountAsync(string accountHash);

    Task<ValidatedFriendsList?> GetValidatedFriendsListAsync(string accountHash);

    Task<RunescapeAccount> UpdateRunescapeAccountAsync(
        string accountHash,
        string displayName
    );
    Task<IDictionary<string, RunescapeAccount>> GetRunescapeAccountsAsync(IList<string> accountHashes);
    Task<ActivityUpdate> InsertActivityUpdateAsync(ActivityUpdate update);
    Task<RunescapeAccount> CreateOrUpdateRunescapeAccountAsync(RunescapeAccount account, string? etag);

    Task<InGameFriendsList?> GetInGameFriendsListAsync(string displayName);
    Task UpdateInGameFriendsListAsync(InGameFriendsList friendsList);
    Task DeleteInGameFriendsListAsync(string displayName, string accountHash);

    Task<(ValidatedFriendsList, string)?> GetValidatedFriendsListWithEtagAsync(string accountHash);
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
        string accountHash, 
        string displayName
    )
    {
        string displayNamePath = RunescapeAccount.DisplayNamePath();

        PatchOperation displayNameOperation = PatchOperation.Set(displayNamePath, displayName);

        return await _accountsContainer.PatchItemAsync<RunescapeAccount>(
            accountHash,
            new(accountHash),
            new[] { displayNameOperation }
        );
    }

    async Task<RunescapeAccount> IDatabaseService.CreateOrUpdateRunescapeAccountAsync(RunescapeAccount account, string? etag)
    {
        if (etag is null)
        {
            return await _accountsContainer.CreateItemAsync(account, new(account.PartitionKey));
        }
        else
        {
            return await _accountsContainer.ReplaceItemAsync(
                account,
                account.AccountHash,
                new(account.PartitionKey),
                new ItemRequestOptions { IfMatchEtag = etag }
            );
        }
    }

    public async Task<RunescapeAccount?> GetRunescapeAccountAsync(string accountHash)
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

    public async Task<ValidatedFriendsList?> GetValidatedFriendsListAsync(string accountHash)
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
    public async Task<(RunescapeAccount, string)?> GetRunescapeAccountWithEtagAsync(string accountHash)
    {
        try
        {
            ItemResponse<RunescapeAccount> response = await _accountsContainer.ReadItemAsync<RunescapeAccount>(accountHash, new(accountHash));

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

    async Task<IDictionary<string, RunescapeAccount>> IDatabaseService.GetRunescapeAccountsAsync(IList<string> accountHashes)
    {

        IReadOnlyList<(string, PartitionKey)> queryItems = accountHashes.Select<string, (string, PartitionKey)>(accountHash =>
        {
            return (accountHash, new(accountHash));
        }).ToList();

        IDictionary<string, RunescapeAccount> results = new Dictionary<string, RunescapeAccount>();

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

    async Task IDatabaseService.DeleteInGameFriendsListAsync(string displayName, string accountHash)
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

    public async Task<(ValidatedFriendsList, string)?> GetValidatedFriendsListWithEtagAsync(string accountHash)
    {
        try
        {
            ItemResponse<ValidatedFriendsList> response = await _validatedFriendsListContainer.ReadItemAsync<ValidatedFriendsList>(
                accountHash, 
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
                friendsList.AccountHash,
                new(friendsList.AccountHash),
                new ItemRequestOptions { IfMatchEtag = etag }
            );
        }
    }
}
