using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OSRSFriendMonitor.Shared.Services.Account.Queue;
using OSRSFriendMonitor.Shared.Services.Account.Queue.Messages;
using OSRSFriendMonitor.Shared.Services.Database;
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

        var account = await accountTask;
        var validatedFriendsList = await validatedFriendsListTask;

        if (account is null)
        {
            throw new InvalidOperationException($"unable to find account with hash {request.AccountHash}");
        }

        var inGameFriendsList = await _databaseService.GetInGameFriendsListAsync(account.DisplayName);

        if (inGameFriendsList is null)
        {
            throw new InvalidOperationException($"unable to find in game friends list for display name {account.DisplayName}, account hash {request.AccountHash}");
        }

        if (validatedFriendsList is null)
        {

        }
    }

    private async Task ProcessAdded()
    {

    }
}

