using Microsoft.Azure.Cosmos;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Diagnostics;

namespace OSRSFriendMonitor.Shared.Services.Database;

public interface IDatabaseService
{
    Task<UserAccount?> GetUserAccountAsync(string userId);
    Task<RunescapeAccount?> GetRunescapeAccountAsync(RunescapeAccountIdentifier id);
    Task<IDictionary<RunescapeAccountIdentifier, RunescapeAccount>> GetRunescapeAccountsAsync(IList<RunescapeAccountIdentifier> ids);
    Task<UserAccount> CreateAccountAsync(UserAccount newAccount);
    Task<ActivityUpdate> InsertActivityUpdateAsync(ActivityUpdate update);
    Task<RunescapeAccount> CreateOrUpdateRunescapeAccountDisplayNameAsync(RunescapeAccountIdentifier id, string displayName);
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

    async Task<RunescapeAccount> IDatabaseService.CreateOrUpdateRunescapeAccountDisplayNameAsync(RunescapeAccountIdentifier id, string displayName)
    {
        RunescapeAccount? accountFromDatabase = await GetRunescapeAccountAsync(id);

        if (accountFromDatabase is null)
        {
            RunescapeAccount newAccount = RunescapeAccount.Create(id, displayName);
            return await _accountsContainer.CreateItemAsync(newAccount, new(newAccount.PartitionKey));
        }
        else if (accountFromDatabase.DisplayName != displayName)
        {
            string patchPath = RunescapeAccount.DisplayNamePath();
            PatchOperation operation = PatchOperation.Set(patchPath, displayName);
            return await _accountsContainer.PatchItemAsync<RunescapeAccount>(
                id.CombinedIdentifier(), 
                new(accountFromDatabase.PartitionKey), 
                new[] { operation }
            );
        } 
        else 
        {
            return accountFromDatabase;
        }
    }
    public async Task<RunescapeAccount?> GetRunescapeAccountAsync(RunescapeAccountIdentifier id)
    {
        try
        {
            return await _accountsContainer.ReadItemAsync<RunescapeAccount>(id.CombinedIdentifier(), new(id.UserId));
        }
        catch (Exception)
        {
            return null;
        }
    }

    async Task<IDictionary<RunescapeAccountIdentifier, RunescapeAccount>> IDatabaseService.GetRunescapeAccountsAsync(IList<RunescapeAccountIdentifier> ids)
    {
        IReadOnlyList<(string, PartitionKey)> queryItems = ids.Select<RunescapeAccountIdentifier, (string, PartitionKey)>(id =>
        {
            return (id.CombinedIdentifier(), new(id.UserId));
        }).ToList();

        IDictionary<RunescapeAccountIdentifier, RunescapeAccount> results = new Dictionary<RunescapeAccountIdentifier, RunescapeAccount>();

        try
        {
            FeedResponse<RunescapeAccount> feed = await _accountsContainer.ReadManyItemsAsync<RunescapeAccount>(queryItems);

            foreach (var account in feed)
            {
                results[account.AccountIdentifier] = account;
            }
        }
        catch (Exception)
        {

        }

        return results;
    }
}
