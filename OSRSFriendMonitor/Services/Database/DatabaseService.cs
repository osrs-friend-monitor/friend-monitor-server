using Microsoft.Azure.Cosmos;
using OSRSFriendMonitor.Services.Database.Models;
using System.Diagnostics;

namespace OSRSFriendMonitor.Services.Database;

public interface IDatabaseService
{
    Task CreateAccount(UserAccount newAccount);
    Task CreateOrUpdateRunescapeAccount(string userId, RunescapeAccount account);
}

internal class DatabaseService : IDatabaseService
{
    private readonly Container _accountsContainer;
    internal DatabaseService(Container accountsContainer)
    {
        _accountsContainer = accountsContainer;
    }
    
    async Task IDatabaseService.CreateAccount(UserAccount newAccount)
    {
        try
        {
            var result = await _accountsContainer.CreateItemAsync(newAccount, new(newAccount.Id));
            Debug.WriteLine(result.RequestCharge);
        }
        catch (CosmosException ex)
        {
            if (ex.StatusCode != System.Net.HttpStatusCode.Conflict)
            {
                throw ex;
            }
        }
    }

    async Task<UserAccount?> GetUserAccount(string id)
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

    async Task IDatabaseService.CreateOrUpdateRunescapeAccount(string userId, RunescapeAccount account)
    {
        UserAccount? user = await GetUserAccount(userId);

        if (user is null)
        {
            throw new ArgumentException($"Unable to find user by id {userId}");
        }

        // gets index and value of the runescape account in the user that has the same account ID as the parameter 
        // "account" passed in to this method.
        var matchingRunescapeAccountResult = user.RunescapeAccounts.Select((value, index) => new { Value=value, Index=index })
            .Where(element => element.Value.AccountHash == account.AccountHash).FirstOrDefault();
                    
        // no matching runescape account for this user, create new one
        if (matchingRunescapeAccountResult is null)
        {
            string patchPath = UserAccount.RunescapeAccountPath(null);

            PatchOperation operation = PatchOperation.Add(patchPath, account);

            UserAccount updatedAccount = await _accountsContainer.PatchItemAsync<UserAccount>(userId, new(userId), new[] { operation });
        } 
        // there is a matching runescape account for this user and it's identical to the one passed in
        else if (matchingRunescapeAccountResult.Value == account)
        {
            return;
        }
        // there is a matching runescape account for this user, but it's not identical (likely display name change)
        else
        {
            string patchPath = UserAccount.RunescapeAccountPath(matchingRunescapeAccountResult.Index);

            PatchOperation operation = PatchOperation.Set(patchPath, account);

            UserAccount updatedAccount = await _accountsContainer.PatchItemAsync<UserAccount>(userId, new(userId), new[] { operation });
        }
    }
}
