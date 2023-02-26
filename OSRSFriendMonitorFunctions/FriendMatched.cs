using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OSRSFriendMonitor.Shared.Services.Account.Queue.Messages;
using OSRSFriendMonitor.Shared.Services.Database;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OSRSFriendMonitorFunctions;

public class FriendMatched
{
    private readonly ILogger _logger;
    private readonly IDatabaseService _databaseService;

    public FriendMatched(ILoggerFactory loggerFactory, IDatabaseService databaseService)
    {
        _logger = loggerFactory.CreateLogger<FriendMatched>();
        _databaseService = databaseService;
    }

    [Function("FriendMatched")]
    public async Task Run([QueueTrigger("%FriendMatchedQueueName%", Connection = "QueueStorageAccountConnectionString")] string myQueueItem)
    {
        PotentialFriendAddition? addition = null;

        try
        {
            addition = JsonSerializer.Deserialize(myQueueItem, QueueMessageJsonContext.Default.PotentialFriendAddition);
            if (addition is null)
            {
                throw new JsonException("why is this null");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to deserialize queue item {queueItem}", myQueueItem);
            return;
        }

        var sendingListTask = _databaseService.GetValidatedFriendsListWithEtagAsync(addition.SendingAccountHash);
        var receivingListTask = _databaseService.GetValidatedFriendsListWithEtagAsync(addition.ReceivingAccountHash);
        var sendingAccountTask = _databaseService.GetRunescapeAccountAsync(addition.SendingAccountHash);
        var receivingAccountTask = _databaseService.GetRunescapeAccountAsync(addition.ReceivingAccountHash);

        var sendingListResult = await sendingListTask;
        var receivingListResult = await receivingListTask;
        var sendingAccount = await sendingAccountTask;
        var receivingAccount = await receivingAccountTask;

        if (sendingListResult is null)
        {
            _logger.LogError("Unable to fetch validated friends list for account hash {sendingAccountHash}", addition.SendingAccountHash);
            return;
        }

        if (receivingListResult is null)
        {
            _logger.LogError("Unable to fetch validated friends list for account hash {receivingAccountHash}", addition.ReceivingAccountHash);
            return;
        }

        if (sendingAccount is null)
        {
            _logger.LogError("Unable to fetch account for account hash {sendingAccountHash}", addition.SendingAccountHash);
            return;
        }

        if (receivingAccount is null)
        {
            _logger.LogError("Unable to fetch account for account hash {receivingAccountHash}", addition.ReceivingAccountHash);
            return;
        }

        ValidatedFriendsList sendingFriendsList = sendingListResult.Value.Item1;
        ValidatedFriendsList receivingFriendsList = receivingListResult.Value.Item1;

        var sendingInGameListTask = _databaseService.GetInGameFriendsListAsync(sendingAccount.DisplayName);
        var receivingInGameListTask = _databaseService.GetInGameFriendsListAsync(receivingAccount.DisplayName);

        var sendingInGameList = await sendingInGameListTask;
        var receivingInGameList = await receivingInGameListTask;

        if (sendingInGameList is null)
        {
            _logger.LogError("Unable to fetch in game friends list for account hash {sendingAccountHash}", addition.SendingAccountHash);
            return;
        }

        if (receivingInGameList is null)
        {
            _logger.LogError("Unable to fetch in game friends list for account hash {receivingAccountHash}", addition.ReceivingAccountHash);
            return;
        }

        if (!sendingInGameList.FriendDisplayNames.Contains(receivingAccount.DisplayName))
        {
            _logger.LogWarning(
                "Receiving account hash {receivingAccountHash} doesn't have its display name {receivingDisplayName} in sending account hash {sendingAccountHash}'s friends list",
                addition.ReceivingAccountHash,
                receivingAccount.DisplayName,
                addition.SendingAccountHash
            );

            return;
        }

        _logger.LogInformation(
            "Attempting to add account hash {sendingAccountHash} to friends list of receiving account hash {receivingAccountHash} if they're friends",
            addition.SendingAccountHash,
            addition.ReceivingAccountHash
        );

        ValidatedFriend? friend = receivingFriendsList.Friends
            .Where(friend => friend.DisplayName == sendingAccount.DisplayName)
            .FirstOrDefault();

        if (friend is null)
        {
            _logger.LogInformation(
                "Couldn't find sending account display name {sendingDisplayName} in receiving account hash {receivingAccountHash}'s validated friends list",
                sendingAccount.DisplayName,
                addition.ReceivingAccountHash
            );

            return;
        }

        if (friend.LastUpdated > addition.Time)
        {
            _logger.LogInformation("Friend last updated is more recent than this message. Not attempting to add friend.");
            return;
        }

        ValidatedFriend updatedFriend = friend with
        {
            AccountHash = addition.SendingAccountHash,
            LastUpdated = DateTime.UtcNow
        };

        HashSet<ValidatedFriend> updatedFriendSet = receivingFriendsList.Friends.ToHashSet();
        updatedFriendSet.Remove(friend);
        updatedFriendSet.Add(updatedFriend);

        ValidatedFriendsList updatedFriendsList = receivingFriendsList with
        {
            Friends = updatedFriendSet.ToImmutableHashSet()
        };

        await _databaseService.UpdateValidatedFriendsListAsync(updatedFriendsList, receivingListResult.Value.Item2);
    }
}
