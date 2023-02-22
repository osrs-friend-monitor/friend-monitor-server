using System.Text.Json;
using Azure.Storage.Queues;
using OSRSFriendMonitor.Shared.Services.Account.Queue.Messages;

namespace OSRSFriendMonitor.Shared.Services.Account.Queue;

public interface IValidatedFriendsListUpdateRequestWriter 
{
    Task Enqueue(string accountHash);
}

public sealed class ValidatedFriendsListUpdateRequestWriter : IValidatedFriendsListUpdateRequestWriter
{
    private readonly QueueClient _queueClient;

    public ValidatedFriendsListUpdateRequestWriter(QueueClient queueClient)
    { 
        _queueClient = queueClient;
    }

    public async Task Enqueue(string accountHash)
    {
        string message = JsonSerializer.Serialize(
            new ValidatedFriendsListUpdateRequest(accountHash, DateTime.UtcNow),
            QueueMessageJsonContext.Default.ValidatedFriendsListUpdateRequest
        );

        await _queueClient.SendMessageAsync(message);
    }
}