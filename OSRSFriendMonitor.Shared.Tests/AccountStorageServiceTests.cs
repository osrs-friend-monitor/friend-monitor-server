//using Moq;
//using OSRSFriendMonitor.Shared.Services.Account;
//using OSRSFriendMonitor.Shared.Services.Database;
//using OSRSFriendMonitor.Shared.Services.Database.Models;
//using System.Collections.Immutable;

//namespace OSRSFriendMonitor.Shared.Tests;

//[TestClass]
//public class AccountStorageServiceTests
//{
//#nullable disable
//    private Mock<IAccountCache> _accountCache;
//    private Mock<Action<RunescapeAccountFriendUpdateRequest>> _friendUpdateRequestAction;
//    private AccountStorageService _accountStorageService;
//    private Mock<IDatabaseService> _databaseService;
//#nullable enable

//    private static readonly RunescapeAccount ACCOUNT_ONE = new(
//        AccountHash: Guid.NewGuid().ToString(),
//        UserId: Guid.NewGuid().ToString(),
//        DisplayName: "Account One"
//    );

//    private static readonly RunescapeAccount ACCOUNT_TWO = new(
//        AccountHash: Guid.NewGuid().ToString(),
//        UserId: Guid.NewGuid().ToString(),
//        DisplayName: "Account Two"
//    );

//    [TestInitialize]
//    public void Setup()
//    {
//        _accountCache = new Mock<IAccountCache>();
//        _databaseService = new Mock<IDatabaseService>();
//        _friendUpdateRequestAction = new Mock<Action<RunescapeAccountFriendUpdateRequest>>();
//        _accountStorageService = new AccountStorageService(_accountCache.Object, _databaseService.Object, _friendUpdateRequestAction.Object);
//    }

//    [TestMethod]
//    public async Task Test_AccountStorageService_GetsRunescapeAccountFromCache()
//    {
//        _accountCache.Setup(c => c.GetRunescapeAccountAsync(ACCOUNT_ONE.AccountHash)).ReturnsAsync(ACCOUNT_ONE);

//        RunescapeAccount? account = await _accountStorageService.GetRunescapeAccountAsync(ACCOUNT_ONE.AccountHash); 
//        Assert.IsNotNull(account);
//        Assert.AreEqual(ACCOUNT_ONE, account);

//        _accountCache.Verify(c => c.GetRunescapeAccountAsync(ACCOUNT_ONE.AccountHash), Times.Once);
//    }

//    [TestMethod]
//    public async Task Test_AccountStorageService_WithCacheMiss_FetchesFromDatabase()
//    {
//        _accountCache.Setup(c => c.GetRunescapeAccountAsync(It.IsAny<string>()))
//            .ReturnsAsync((RunescapeAccount?)null);

//        _databaseService.Setup(d => d.GetRunescapeAccountAsync(ACCOUNT_ONE.AccountHash))
//            .ReturnsAsync(ACCOUNT_ONE);

//        RunescapeAccount? account = await _accountStorageService.GetRunescapeAccountAsync(ACCOUNT_ONE.AccountHash);
//        Assert.IsNotNull(account);
//        Assert.AreEqual(ACCOUNT_ONE, account);

//        _accountCache.Verify(c => c.GetRunescapeAccountAsync(ACCOUNT_ONE.AccountHash), Times.Once);
//        _accountCache.Verify(c => c.AddRunescapeAccount(account), Times.Once);

//        _databaseService.Verify(d => d.GetRunescapeAccountAsync(ACCOUNT_ONE.AccountHash), Times.Once);
//    }

//    [TestMethod]
//    public async Task Test_AccountStorageService_WithCacheMissAndDatabaseMiss_ReturnsNull()
//    {
//        _accountCache.Setup(c => c.GetRunescapeAccountAsync(It.IsAny<string>()))
//            .ReturnsAsync((RunescapeAccount?)null);

//        _databaseService.Setup(d => d.GetRunescapeAccountAsync(ACCOUNT_ONE.AccountHash))
//            .ReturnsAsync((RunescapeAccount?)null);

//        RunescapeAccount? account = await _accountStorageService.GetRunescapeAccountAsync(ACCOUNT_ONE.AccountHash);
//        Assert.IsNull(account);
//    }

//    [TestMethod]
//    public async Task Test_AccountStorageService_CreateOrUpdate_CreateNewAccount()
//    {
//        _accountCache.Setup(c => c.GetRunescapeAccountAsync(ACCOUNT_ONE.AccountHash))
//            .ReturnsAsync((RunescapeAccount?)null);

//        _databaseService.Setup(d => d.GetRunescapeAccountAsync(ACCOUNT_ONE.AccountHash))
//            .ReturnsAsync((RunescapeAccount?)null);

//        // TODO: figure out how to make it match the datetime here because this isn't getting hit ???
//        _databaseService.Setup(d => d.CreateOrUpdateRunescapeAccountAsync(ACCOUNT_ONE, null)).ReturnsAsync(ACCOUNT_ONE);

//        RunescapeAccount? result = await _accountStorageService.CreateRunescapeAccountOrUpdateAsync(
//            accountHash: ACCOUNT_ONE.AccountHash,
//            userId: ACCOUNT_ONE.UserId,
//            displayName: ACCOUNT_ONE.DisplayName,
//            friends: new[] { ACCOUNT_TWO.DisplayName }
//        );

//        Assert.IsNotNull(result);
//        Assert.AreEqual(ACCOUNT_ONE, result);
//        _accountCache.Verify(c => c.AddRunescapeAccount(ACCOUNT_ONE), Times.Once);
//    }

//    //[TestMethod]
//    //public async Task Test_AccountStorageService_CreateOrUpdate_DifferentUserId_Throws()
//    //{
//    //    _accountCache.Setup(c => c.GetRunescapeAccountAsync(ACCOUNT_ONE.AccountHash))
//    //        .ReturnsAsync(
//    //            ACCOUNT_ONE with
//    //            {
//    //                UserId = Guid.NewGuid().ToString()
//    //            }
//    //        );

//    //    await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => 
//    //        _accountStorageService.CreateRunescapeAccountOrUpdateNameAsync(
//    //            accountHash: ACCOUNT_ONE.AccountHash,
//    //            userId: ACCOUNT_ONE.UserId,
//    //            displayName: ACCOUNT_ONE.DisplayName,
//    //            previousDisplayName: ACCOUNT_ONE.PreviousName
//    //        )
//    //    );

//    //    _databaseService.VerifyNoOtherCalls();
//    //    _accountCache.Verify(c => c.AddRunescapeAccount(It.IsAny<RunescapeAccount>()), Times.Never);
//    //}

//    //[TestMethod]
//    //public async Task Test_AccountStorageService_CreateOrUpdate_NoNeedToUpdate_DoesNothing()
//    //{
//    //    _accountCache.Setup(c => c.GetRunescapeAccountAsync(ACCOUNT_ONE.AccountHash))
//    //        .ReturnsAsync(ACCOUNT_ONE);

//    //    RunescapeAccount? result = await _accountStorageService.CreateRunescapeAccountOrUpdateNameAsync(
//    //        accountHash: ACCOUNT_ONE.AccountHash,
//    //        displayName: ACCOUNT_ONE.DisplayName,
//    //        userId: ACCOUNT_ONE.UserId,
//    //        previousDisplayName: ACCOUNT_ONE.PreviousName
//    //    );

//    //    Assert.IsNotNull(result);
//    //    Assert.AreEqual(ACCOUNT_ONE, result);

//    //    _databaseService.Verify(
//    //        d => d.UpdateRunescapeAccountDisplayNameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), 
//    //        Times.Never
//    //    );

//    //    _accountCache.Verify(c => c.AddRunescapeAccount(It.IsAny<RunescapeAccount>()), Times.Never);
//    //}

//    //[TestMethod]
//    //public async Task Test_AccountStorageService_CreateOrUpdate_UpdateDisplayName_WorksCorrectly()
//    //{
//    //    RunescapeAccount accountWithUpdatedName = ACCOUNT_ONE with
//    //    {
//    //        DisplayName = ACCOUNT_ONE.DisplayName + "new",
//    //    };

//    //    _accountCache.Setup(c => c.GetRunescapeAccountAsync(ACCOUNT_ONE.AccountHash))
//    //        .ReturnsAsync(ACCOUNT_ONE);

//    //    _databaseService.Setup(
//    //        d => d.UpdateRunescapeAccountDisplayNameAsync(
//    //            accountWithUpdatedName.AccountHash,
//    //            accountWithUpdatedName.DisplayName,
//    //            accountWithUpdatedName.PreviousName
//    //        )
//    //    )
//    //        .ReturnsAsync(accountWithUpdatedName);

//    //    RunescapeAccount? result = await _accountStorageService.CreateRunescapeAccountOrUpdateNameAsync(
//    //        accountHash: accountWithUpdatedName.AccountHash,
//    //        displayName: accountWithUpdatedName.DisplayName,
//    //        userId: accountWithUpdatedName.UserId,
//    //        previousDisplayName: accountWithUpdatedName.PreviousName
//    //    );

//    //    Assert.IsNotNull(result);
//    //    Assert.AreEqual(accountWithUpdatedName, result);

//    //    _accountCache.Verify(c => c.GetRunescapeAccountAsync(ACCOUNT_ONE.AccountHash), Times.Once);
//    //    _accountCache.Verify(c => c.AddRunescapeAccount(accountWithUpdatedName), Times.Once);
//    //}

//    [TestMethod]
//    public async Task Test_AccountStorageService_GetRunescapeAccounts_GetsSomeAccountsFromCacheAndSomeFromDatabase()
//    {
//        _accountCache.Setup(c => c.GetRunescapeAccountsAsync(It.IsAny<IList<string>>()))
//            .ReturnsAsync((
//                new Dictionary<string, RunescapeAccount> { { ACCOUNT_ONE.AccountHash, ACCOUNT_ONE } }, 
//                new List<string> { ACCOUNT_TWO.AccountHash }
//            ));

//        _databaseService.Setup(d => d.GetRunescapeAccountsAsync(It.Is<IList<string>>(l => l.Count == 1 && l.First() == ACCOUNT_TWO.AccountHash)))
//            .ReturnsAsync(new Dictionary<string, RunescapeAccount> { { ACCOUNT_TWO.AccountHash, ACCOUNT_TWO } });

//        IDictionary<string, RunescapeAccount> results = await _accountStorageService.GetRunescapeAccountsAsync(
//            new List<string> { ACCOUNT_ONE.AccountHash, ACCOUNT_TWO.AccountHash }
//        );

//        Assert.AreEqual(2, results.Count);
//        Assert.AreEqual(ACCOUNT_ONE, results[ACCOUNT_ONE.AccountHash]);
//        Assert.AreEqual(ACCOUNT_TWO, results[ACCOUNT_TWO.AccountHash]);

//        _accountCache.Verify(c => c.GetRunescapeAccountsAsync(It.IsAny<IList<string>>()), Times.Once);
//        _accountCache.Verify(c => c.AddRunescapeAccount(ACCOUNT_TWO), Times.Once);
//        _accountCache.VerifyNoOtherCalls();

//        _databaseService.Verify(d => d.GetRunescapeAccountsAsync(It.IsAny<IList<string>>()), Times.Once);
//        _databaseService.VerifyNoOtherCalls();
//    }
//}
