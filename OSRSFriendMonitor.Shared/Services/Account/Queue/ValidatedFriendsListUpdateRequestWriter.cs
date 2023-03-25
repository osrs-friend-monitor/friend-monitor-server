using Azure.Storage.Queues;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace OSRSFriendMonitor.Shared.Services.Account.Queue;

public interface IQueueWriter <T>
{
    Task EnqueueMessageAsync(T message);
    Task EnqueueMessageAsync(T message, JsonTypeInfo<T> jsonTypeInfo);
}

public sealed class QueueWriter<T> : IQueueWriter<T>
{
    private readonly QueueClient _queueClient;

    public QueueWriter(QueueClient queueClient)
    { 
        _queueClient = queueClient;
    }

    public async Task EnqueueMessageAsync(T message)
    {
        string messageText = JsonSerializer.Serialize<T>(message);
        await RetryingTask(() => _queueClient.SendMessageAsync(messageText));
    }

    public async Task EnqueueMessageAsync(T message, JsonTypeInfo<T> jsonTypeInfo)
    {
        string messageText = JsonSerializer.Serialize(message, jsonTypeInfo);

        await RetryingTask(() => _queueClient.SendMessageAsync(messageText));
    }

    private static async Task RetryingTask(Func<Task> task, int count = 3)
    {

        for (int i = 0; i < count - 1; i++)
        {
            try
            {
                await task();
            }
            catch
            {
                await Task.Delay(200);
            }
        }

        await task();
    }
}