using Microsoft.Azure.Cosmos;
using OSRSFriendMonitor.Shared.Services.Database.Models;

namespace OSRSFriendMonitor.Shared.Services.Database;

public interface IDatabaseService
{
    Task<UserAccount?> GetUserAccountAsync(string userId);
    Task<(RunescapeAccount, string)?> GetRunescapeAccountAsync(string accountHash);
    Task<IDictionary<string, RunescapeAccount>> GetRunescapeAccountsAsync(IList<string> accountHashes);
    Task<UserAccount> CreateAccountAsync(UserAccount newAccount);
    Task<ActivityUpdate> InsertActivityUpdateAsync(ActivityUpdate update);
    Task<RunescapeAccount> CreateOrUpdateRunescapeAccountAsync(RunescapeAccount account, string? etag);
}

public class DatabaseService : IDatabaseService
{
    private readonly Container _accountsContainer;
    private readonly Container _activityContainer;

    public DatabaseService(Container accountsContainer, Container activityContainer)
    {
        _accountsContainer = accountsContainer;
        _activityContainer = activityContainer;
    }

    async Task<ActivityUpdate> IDatabaseService.InsertActivityUpdateAsync(ActivityUpdate update) {
        return await _activityContainer.CreateItemAsync(update, new(update.PartitionKey));
    }
    
    async Task<UserAccount> IDatabaseService.CreateAccountAsync(UserAccount newAccount)
    {
        try
        {
            return await _accountsContainer.CreateItemAsync(newAccount, new(newAccount.Id));
        }
        catch (CosmosException ex)
        {
            if (ex.StatusCode != System.Net.HttpStatusCode.Conflict)
            {
                throw ex;
            }
            else 
            {
                return (await GetUserAccountAsync(newAccount.Id))!;
            }
        }
    }

    public async Task<UserAccount?> GetUserAccountAsync(string id)
    {
        try
        {
            return await _accountsContainer.ReadItemAsync<UserAccount>(id, new(id));
        }
        catch (Exception)
        {
            return null;
        }
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
    public async Task<(RunescapeAccount, string)?> GetRunescapeAccountAsync(string accountHash)
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
