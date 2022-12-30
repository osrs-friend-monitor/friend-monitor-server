using Microsoft.Azure.Cosmos;
using OSRSFriendMonitor.Shared.Services.Database.Models;

namespace OSRSFriendMonitor.Shared.Services.Database;

public interface IDatabaseService
{
    Task<(RunescapeAccount, string)?> GetRunescapeAccountWithEtagAsync(string accountHash);
    Task<RunescapeAccount?> GetRunescapeAccountAsync(string accountHash);
    Task<RunescapeAccount> UpdateRunescapeAccountDisplayNameAsync(
         string accountHash,
         string displayName,
         string? previousDisplayName
     );
    Task<IDictionary<string, RunescapeAccount>> GetRunescapeAccountsAsync(IList<string> accountHashes);
    Task<ActivityUpdate> InsertActivityUpdateAsync(ActivityUpdate update);
    Task<RunescapeAccount> CreateOrUpdateRunescapeAccountAsync(RunescapeAccount account, string? etag);
}

public class DatabaseService : IDatabaseService
{
    private readonly Container _accountsContainer;
    private readonly Container _activityContainer;
    private readonly Container _friendRequestsContainer;

    public DatabaseService(Container accountsContainer, Container activityContainer, Container friendRequestsContainer)
    {
        _accountsContainer = accountsContainer;
        _activityContainer = activityContainer;
        _friendRequestsContainer = friendRequestsContainer;
     }

    async Task<ActivityUpdate> IDatabaseService.InsertActivityUpdateAsync(ActivityUpdate update) {
        return await _activityContainer.CreateItemAsync(update, new(update.PartitionKey));
    }

    public async Task<RunescapeAccount> UpdateRunescapeAccountDisplayNameAsync(
        string accountHash, 
        string displayName, 
        string? previousDisplayName
    )
    {
        string displayNamePath = RunescapeAccount.DisplayNamePath();
        string previousDisplayNamePath = RunescapeAccount.PreviousDisplayNamePath();

        PatchOperation displayNameOperation = PatchOperation.Set(displayNamePath, displayName);
        PatchOperation previousDisplayNameOperation = PatchOperation.Set(previousDisplayNamePath, previousDisplayName);

        return await _accountsContainer.PatchItemAsync<RunescapeAccount>(
            accountHash,
            new(accountHash),
            new[] { displayNameOperation, previousDisplayNameOperation }
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
}
