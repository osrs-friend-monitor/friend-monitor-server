using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OSRSFriendMonitor.Shared.Services.Account.Queue;
using OSRSFriendMonitor.Shared.Services.Account.Queue.Messages;
using OSRSFriendMonitor.Shared.Services.Database;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Collections.Immutable;
using System.Text.Json;

namespace OSRSFriendMonitorFunctions;

public class FriendUpdateRequest
{
    private readonly ILogger _logger;
    private readonly IDatabaseService _databaseService;
    private readonly IQueueWriter<FriendMatched> _friendMatchedQueueWriter;
    private readonly IQueueWriter<FriendNoLongerPresent> _friendNoLongerPresentQueueWriter;

    public FriendUpdateRequest(
        ILoggerFactory loggerFactory,
        IDatabaseService databaseService,
        IQueueWriter<FriendMatched> friendMatchedQueueWriter,
        IQueueWriter<FriendNoLongerPresent> friendNoLongerPresentQueueWriter
    )
    {
        _logger = loggerFactory.CreateLogger<FriendUpdateRequest>();
        _databaseService = databaseService;
        _friendMatchedQueueWriter = friendMatchedQueueWriter;
        _friendNoLongerPresentQueueWriter = friendNoLongerPresentQueueWriter;
    }

    [Function("FriendUpdateRequest")]
    public async Task RunAsync([QueueTrigger("%FriendUpdateRequestQueueName%", Connection = "QueueStorageAccountConnectionString")] string myQueueItem)
    {
        ValidatedFriendsListUpdateRequest? request;

        try
        {
            request = JsonSerializer.Deserialize(myQueueItem, QueueMessageJsonContext.Default.ValidatedFriendsListUpdateRequest);
            if (request is null)
            {
                throw new JsonException("why is this null");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to deserialize queue item {queueItem}", myQueueItem);
            return;
        }

        var accountTask = _databaseService.GetRunescapeAccountAsync(request.AccountHash);
        var validatedFriendsListTask = _databaseService.GetValidatedFriendsListWithEtagAsync(request.AccountHash);

        RunescapeAccount? account = await accountTask;
        var validatedFriendsList = await validatedFriendsListTask;

        if (account is null)
        {
            throw new InvalidOperationException($"unable to find account with hash {request.AccountHash}");
        }

        InGameFriendsList? inGameFriendsList = await _databaseService.GetInGameFriendsListAsync(account.DisplayName) ??
            throw new InvalidOperationException($"unable to find in game friends list for display name {account.DisplayName}, account hash {request.AccountHash}");


        IImmutableSet<ValidatedFriend> validatedFriendsListEntries = validatedFriendsList?.Item1.Friends ?? ImmutableHashSet<ValidatedFriend>.Empty;

        IEnumerable<string> addedFriends = inGameFriendsList.FriendDisplayNames
            .Except(validatedFriendsListEntries.Select(x => x.DisplayName));

        IEnumerable<ValidatedFriend> removedFriends = validatedFriendsListEntries.ExceptBy(
            inGameFriendsList.FriendDisplayNames,
            friend => friend.DisplayName
        );

        IEnumerable<ValidatedFriend> unchangedFriends = validatedFriendsListEntries.IntersectBy(
            inGameFriendsList.FriendDisplayNames,
            friend => friend.DisplayName
        );

        // for removed friends with account hashes, enqueue PotentialFriendRemoval events

        // for added friends, fetch in game friends list and if they are friends add the account hash and
        // enqueue potential friend addition

        // for unchanged friends, find ones last updated too long ago and fetch in game friends lists for them all.
        // if there's a change, update. 
    }

    async Task<(IImmutableSet<ValidatedFriend>, PotentialFriendRemoval[])> ProcessRemoved(IEnumerable<ValidatedFriend> removed)
    {

    }

    async Task<(IImmutableSet<ValidatedFriend>, PotentialFriendAddition[])> ProcessAdded(IEnumerable<string> added)
    {

    }
}

