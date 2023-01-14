using Moq;
using OSRSFriendMonitor.Shared.Services.Activity;
using OSRSFriendMonitor.Shared.Services.Cache;
using StackExchange.Redis;
using System.Text.Json;

namespace OSRSFriendMonitor.Shared.Tests;

[TestClass]
public class ActivityCacheTests
{
#nullable disable
    private Mock<IRemoteCache> _mockRemoteCache;
    private ActivityCache _activityCache;
#nullable enable
    private static readonly CachedLocationUpdate UPDATE_ONE = new(
        X: 1000,
        Y: 1001,
        Plane: 1,
        AccountHash: Guid.NewGuid().ToString()
    );

    private static readonly CachedLocationUpdate UPDATE_TWO = new(
        X: 1001,
        Y: 1002,
        Plane: 0,
        AccountHash: Guid.NewGuid().ToString()
    );

    private static readonly string UPDATE_ONE_JSON = JsonSerializer.Serialize(
        UPDATE_ONE, 
        ActivityCacheJsonContext.Default.CachedLocationUpdate
    );

    private static readonly string UPDATE_TWO_JSON = JsonSerializer.Serialize(
        UPDATE_TWO,
        ActivityCacheJsonContext.Default.CachedLocationUpdate
    );

    [TestInitialize]
    public void Setup()
    {
        _mockRemoteCache = new Mock<IRemoteCache>();
        _activityCache = new ActivityCache(_mockRemoteCache.Object);
    }

    [TestMethod]
    public void Test_ActivityCache_AddLocationUpdate()
    {
        _activityCache.AddLocationUpdate(UPDATE_ONE);

        _mockRemoteCache.Verify(
            c => c.SetValueWithoutWaiting(
                It.Is<KeyValuePair<string, string>>(p => p.Key == $"location:{UPDATE_ONE.AccountHash}" && p.Value == UPDATE_ONE_JSON),
                It.IsAny<TimeSpan>()
            ), 
            Times.Once
        );

        _activityCache.AddLocationUpdate(UPDATE_TWO);

        _mockRemoteCache.Verify(
            c => c.SetValueWithoutWaiting(
                It.Is<KeyValuePair<string, string>>(p => p.Key == $"location:{UPDATE_TWO.AccountHash}" && p.Value == UPDATE_TWO_JSON),
                It.IsAny<TimeSpan>()
            ),
            Times.Once
        );
    }

    [TestMethod]
    public async Task Test_ActivityCache_GivenNoResults_ReturnsEmptyDictionary()
    {
        _mockRemoteCache.Setup(c => c.GetMultipleValuesAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(Array.Empty<RedisValue>());

        IDictionary<string, CachedLocationUpdate> results = await _activityCache.GetLocationUpdatesAsync(new List<String>());

        Assert.AreEqual(0, results.Count);
        _mockRemoteCache.Verify(
            c => c.GetMultipleValuesAsync(
                It.Is<IEnumerable<string>>(e => e.Count() == 0)
            ),
            Times.Once
        );
    }

    [TestMethod]
    public async Task Test_ActivityCache_GivenNullResult_IgnoresIt()
    {
        RedisValue resultOne = RedisValue.Null;
        RedisValue resultTwo = UPDATE_TWO_JSON;

        _mockRemoteCache.Setup(
            c => c.GetMultipleValuesAsync(
                It.Is<IEnumerable<string>>(e => e.Count() == 2)
            )
        )
            .ReturnsAsync(new RedisValue[] { resultOne, resultTwo });

        IDictionary<string, CachedLocationUpdate> results = await _activityCache.GetLocationUpdatesAsync(
            new List<String> { UPDATE_ONE.AccountHash, UPDATE_TWO.AccountHash }
        );

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(UPDATE_TWO, results[UPDATE_TWO.AccountHash]);

        _mockRemoteCache.Verify(
            c => c.GetMultipleValuesAsync(
                It.IsAny<IEnumerable<string>>()
            ),
            Times.Once
        );
        _mockRemoteCache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Test_ActivityCache_GivenSingleResult_ReturnsSuccessfully()
    {
        RedisValue result = UPDATE_ONE_JSON;

        _mockRemoteCache.Setup(
            c => c.GetMultipleValuesAsync(
                It.Is<IEnumerable<string>>(e => e.Count() == 1 && e.First() == $"location:{UPDATE_ONE.AccountHash}")
            )
        )
            .ReturnsAsync(new RedisValue[] { result });

        IDictionary<string, CachedLocationUpdate> results = await _activityCache.GetLocationUpdatesAsync(
            new List<String> { UPDATE_ONE.AccountHash }
        );

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(UPDATE_ONE, results[UPDATE_ONE.AccountHash]);

        _mockRemoteCache.Verify(
            c => c.GetMultipleValuesAsync(
                It.IsAny<IEnumerable<string>>()
            ),
            Times.Once
        );
    }

    [TestMethod]
    public async Task Test_ActivityCache_GivenTwoResults_ReturnsThemBoth()
    {
        RedisValue resultOne = UPDATE_ONE_JSON;
        RedisValue resultTwo = UPDATE_TWO_JSON;

        _mockRemoteCache.Setup(
            c => c.GetMultipleValuesAsync(
                It.Is<IEnumerable<string>>(e => e.Count() == 2)
            )
        )
            .ReturnsAsync(new RedisValue[] { resultOne, resultTwo });

        IDictionary<string, CachedLocationUpdate> results = await _activityCache.GetLocationUpdatesAsync(
            new List<String> { UPDATE_ONE.AccountHash, UPDATE_TWO.AccountHash }
        );

        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(UPDATE_ONE, results[UPDATE_ONE.AccountHash]);
        Assert.AreEqual(UPDATE_TWO, results[UPDATE_TWO.AccountHash]);

        _mockRemoteCache.Verify(
            c => c.GetMultipleValuesAsync(
                It.IsAny<IEnumerable<string>>()
            ),
            Times.Once
        );
    }

    [TestMethod]
    public async Task Test_ActivityCache_ResultWithInvalidJson_DoesNotThrow()
    {
        RedisValue resultOne = UPDATE_ONE_JSON;
        RedisValue resultTwo = "{sdlkfasjkldfkljasdf@@@@}";

        _mockRemoteCache.Setup(
            c => c.GetMultipleValuesAsync(
                It.IsAny<IEnumerable<string>>()
            )
        )
            .ReturnsAsync(new RedisValue[] { resultOne, resultTwo });

        IDictionary<string, CachedLocationUpdate> results = await _activityCache.GetLocationUpdatesAsync(
            new List<String> { UPDATE_ONE.AccountHash }
        );

        Assert.AreEqual(1, results.Count);
    }
}
