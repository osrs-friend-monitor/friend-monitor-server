namespace OSRSFriendMonitor.Shared.Services.Database.Models;

public record FriendRequest(
    string Sender,
    string RequestedFriendDisplayName,
    string RequestedFriendAccountHash
)
{
    string PartitionKey => RequestedFriendAccountHash;
}
