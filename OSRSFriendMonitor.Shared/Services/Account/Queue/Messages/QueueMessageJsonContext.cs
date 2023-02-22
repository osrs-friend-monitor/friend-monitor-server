using System.Text.Json.Serialization;

namespace OSRSFriendMonitor.Shared.Services.Account.Queue.Messages;

[JsonSerializable(typeof(PotentialFriendAddition))]
[JsonSerializable(typeof(PotentialFriendRemoval))]
[JsonSerializable(typeof(ValidatedFriendsListUpdateRequest))]
public partial class QueueMessageJsonContext: JsonSerializerContext
{
    
}