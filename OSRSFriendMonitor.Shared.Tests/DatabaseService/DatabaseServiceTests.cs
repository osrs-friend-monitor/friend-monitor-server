using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Cosmos;
using OSRSFriendMonitor.Shared.Services.Database;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Collections.Immutable;
using System.Net;

namespace OSRSFriendMonitor.Shared.Tests.DatabaseService;

[TestClass]
public class DatabaseServiceIntegrationTests
{
    #nullable disable
    public TestContext TestContext{ get; set; }
    IDatabaseService _databaseService;
    #nullable enable
    private string GetProperty(string name)
    {
        object? property = TestContext.Properties[name];

        if (property is string propertyString)
        {
            return propertyString;
        }

        throw new Exception($"Property \"{name}\" not found");
    }

    [TestInitialize]
    public void Setup()
    {
        CosmosClient client = new CosmosClientBuilder(GetProperty("DatabaseConnectionString"))
            .WithCustomSerializer(new SystemTextJsonSerializer())
            .Build();

        Database db = client.GetDatabase("FriendMonitorDatabase");

        Container accountsContainer = db.GetContainer("Accounts");
        Container activityContainer = db.GetContainer("Activity");
        Container friendRequestsContainer = db.GetContainer("FriendRequests");

        _databaseService = new Services.Database.DatabaseService(accountsContainer, activityContainer, friendRequestsContainer);
    }

    [TestMethod]
    public async Task TestCreateAccount()
    {
        string accountHash = Guid.NewGuid().ToString();
        string userId = Guid.NewGuid().ToString();
        string displayName = "new displayname" + accountHash;

        // Create
        RunescapeAccount? accountBeforeCreate = await _databaseService.GetRunescapeAccountAsync(accountHash);

        Assert.IsNull(accountBeforeCreate);

        RunescapeAccount newAccount = new(accountHash, userId, displayName, null, ImmutableList<Friend>.Empty);
        RunescapeAccount newAccountInDatabase = await _databaseService.CreateOrUpdateRunescapeAccountAsync(newAccount, null);

        Assert.AreEqual(newAccount, newAccountInDatabase);

        RunescapeAccount? accountAfterCreate = await _databaseService.GetRunescapeAccountAsync(accountHash);

        Assert.AreEqual(newAccount, accountAfterCreate);

        // Can't create twice
        CosmosException exception = await Assert.ThrowsExceptionAsync<CosmosException>(
            () => _databaseService.CreateOrUpdateRunescapeAccountAsync(newAccount, null)
        );

        Assert.AreEqual(HttpStatusCode.Conflict, exception.StatusCode);

        // Update with etag
        var result = await _databaseService.GetRunescapeAccountWithEtagAsync(accountHash);
        Assert.IsNotNull(result);

        var accountWithEtag = result.GetValueOrDefault().Item1;
        var etag = result.GetValueOrDefault().Item2;

        Assert.AreEqual(newAccount, accountWithEtag);

        string newDisplayName = accountWithEtag.DisplayName + "updated";

        accountWithEtag = accountWithEtag with
        {
            DisplayName = newDisplayName
        };

        RunescapeAccount accountWithUpdatedName = await _databaseService.CreateOrUpdateRunescapeAccountAsync(accountWithEtag, etag);

        Assert.AreEqual(accountWithEtag, accountWithUpdatedName);

        // Can't use same etag again
        CosmosException etagMismatchException = await Assert.ThrowsExceptionAsync<CosmosException>(
            () => _databaseService.CreateOrUpdateRunescapeAccountAsync(accountWithEtag, etag)
        );

        Assert.AreEqual(HttpStatusCode.Conflict, exception.StatusCode);

        // Update name again, this time with dedicated function
        accountWithEtag = accountWithEtag with
        {
            DisplayName = newDisplayName + "_again",
            PreviousName = newDisplayName
        };

        RunescapeAccount accountWithSecondUpdatedName = await _databaseService.UpdateRunescapeAccountDisplayNameAsync(
            accountHash,
            accountWithEtag.DisplayName,
            accountWithEtag.PreviousName
        );

        Assert.AreEqual(accountWithEtag, accountWithSecondUpdatedName);
    }

    [TestMethod]
    public async Task TestMultipleAccountsAsync()
    {
        string accountHash = Guid.NewGuid().ToString();
        string userId = Guid.NewGuid().ToString();
        string displayName = "new displayname" + accountHash;

        RunescapeAccount newAccount = new(accountHash, userId, displayName, null, ImmutableList<Friend>.Empty);
        RunescapeAccount newAccountInDatabase = await _databaseService.CreateOrUpdateRunescapeAccountAsync(newAccount, null);

        IList<string> accountHashes = new List<string>
        {
            accountHash
        };

        IDictionary<string, RunescapeAccount> accountsWithCorrectIds = await _databaseService.GetRunescapeAccountsAsync(accountHashes);

        Assert.AreEqual(1, accountsWithCorrectIds.Count);
        Assert.AreEqual(accountHash, accountsWithCorrectIds.Keys.First());
        Assert.AreEqual(newAccountInDatabase, accountsWithCorrectIds.Values.First());

        accountHashes.Add(accountHash + "askljdfklsja");

        IDictionary<string, RunescapeAccount> accountsWithIncorrectIds = await _databaseService.GetRunescapeAccountsAsync(accountHashes);

        Assert.AreEqual(1, accountsWithIncorrectIds.Count);
        Assert.AreEqual(accountHash, accountsWithIncorrectIds.Keys.First());
        Assert.AreEqual(newAccountInDatabase, accountsWithIncorrectIds.Values.First());
    }

    [TestMethod]
    public async Task TestCreateActivityUpdate()
    {
        ActivityUpdate update = new LocationUpdate(
            X: 1000,
            Y: 1000,
            Plane: 0,
            Id: Guid.NewGuid().ToString(),
            World: 325,
            AccountHash: Guid.NewGuid().ToString(),
            Time: DateTime.Now
        );

        await _databaseService.InsertActivityUpdateAsync(update);
    }
}
