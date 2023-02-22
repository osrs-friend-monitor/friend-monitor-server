using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace OSRSFriendMonitorFunctions;

public class FriendUpdateRequest
{
    private readonly ILogger _logger;

    public FriendUpdateRequest(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<FriendUpdateRequest>();
    }

    [Function("FriendUpdateRequest")]
    public void Run([QueueTrigger("friend-update-request-test", Connection = "queue-storage-connection-string")] string myQueueItem)
    {
        _logger.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
    }
}

