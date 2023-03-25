namespace OSRSFriendMonitor.Shared.Services.Account.Queue.Messages;

public sealed record ValidatedFriendsListUpdateRequest(
    long AccountHash,
    DateTime EnqueueTime
);