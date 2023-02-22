namespace OSRSFriendMonitor.Shared.Services.Account.Queue.Messages;

public sealed record ValidatedFriendsListUpdateRequest(
    string AccountHash,
    DateTime EnqueueTime
);