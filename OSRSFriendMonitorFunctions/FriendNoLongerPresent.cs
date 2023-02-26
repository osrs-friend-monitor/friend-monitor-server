using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OSRSFriendMonitor.Shared.Services.Account.Queue.Messages;
using OSRSFriendMonitor.Shared.Services.Database;
using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Collections.Immutable;
using System.Text.Json;

namespace OSRSFriendMonitorFunctions;

public class FriendNoLongerPresent
{
    private readonly ILogger _logger;
    private readonly IDatabaseService _databaseService;
    public FriendNoLongerPresent(ILoggerFactory loggerFactory, IDatabaseService databaseService)
    {
        _logger = loggerFactory.CreateLogger<FriendNoLongerPresent>();
        _databaseService = databaseService;
    }

    [Function("FriendNoLongerPresent")]
    public async Task Run([QueueTrigger("%FriendNoLongerPresentQueueName%", Connection = "QueueStorageAccountConnectionString")] string myQueueItem)
    {
        PotentialFriendRemoval? removal = null;

        try
        {
            removal = JsonSerializer.Deserialize(myQueueItem, QueueMessageJsonContext.Default.PotentialFriendRemoval);
            if (removal is null)
            {
                throw new JsonException("why is this null");
            }
        } 
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Unable to deserialize queue item {queueItem}", myQueueItem);
            return;
        }

        var sendingListTask = _databaseService.GetValidatedFriendsListWithEtagAsync(removal.SendingAccountHash);
        var receivingListTask = _databaseService.GetValidatedFriendsListWithEtagAsync(removal.ReceivingAccountHash);
        var sendingAccountTask = _databaseService.GetRunescapeAccountAsync(removal.SendingAccountHash);
        var receivingAccountTask = _databaseService.GetRunescapeAccountAsync(removal.ReceivingAccountHash);

        var sendingListResult = await sendingListTask;
        var receivingListResult = await receivingListTask;
        var sendingAccount = await sendingAccountTask;
        var receivingAccount = await receivingAccountTask;

        if (sendingListResult is null)
        {
            _logger.LogError("Unable to fetch validated friends list for account hash {sendingAccountHash}", removal.SendingAccountHash);
            return;
        }

        if (receivingListResult is null)
        {
            _logger.LogError("Unable to fetch validated friends list for account hash {receivingAccountHash}", removal.ReceivingAccountHash);
            return;
        }

        if (sendingAccount is null)
        {
            _logger.LogError("Unable to fetch account for account hash {sendingAccountHash}", removal.SendingAccountHash);
            return;
        }

        if (receivingAccount is null)
        {
            _logger.LogError("Unable to fetch account for account hash {receivingAccountHash}", removal.ReceivingAccountHash);
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
            _logger.LogError("Unable to fetch in game friends list for account hash {sendingAccountHash}", removal.SendingAccountHash);
            return;
        }

        if (receivingInGameList is null)
        {
            _logger.LogError("Unable to fetch in game friends list for account hash {receivingAccountHash}", removal.ReceivingAccountHash);
            return;
        }

        if (sendingInGameList.FriendDisplayNames.Contains(receivingAccount.DisplayName))
        {
            _logger.LogWarning(
                "Receiving account hash {receivingAccountHash} unexpectedly has its display name {receivingDisplayName} in sending account hash {sendingAccountHash}'s friends list",
                removal.ReceivingAccountHash,
                receivingAccount.DisplayName,
                removal.SendingAccountHash
            );

            return;
        }

        _logger.LogInformation(
            "Attempting to remove account hash {sendingAccountHash} from friends list of receiving account hash {receivingAccountHash}",
            removal.SendingAccountHash,
            removal.ReceivingAccountHash
        );

        ValidatedFriend? friend = receivingFriendsList.Friends
            .Where(friend => friend.DisplayName == sendingAccount.DisplayName)
            .FirstOrDefault();

        if (friend is null)
        {
            _logger.LogWarning(
                "Couldn't find sending account display name {sendingDisplayName} in receiving account hash {receivingAccountHash}'s validated friends list",
                sendingAccount.DisplayName,
                removal.ReceivingAccountHash
            );

            return;
        }

        if (friend.LastUpdated > removal.Time)
        {
            _logger.LogInformation("Friend last updated is more recent than this message. Not attempting to remove friend.");
            return;
        }

        ValidatedFriend updatedFriend = friend with
        {
            AccountHash = null,
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
