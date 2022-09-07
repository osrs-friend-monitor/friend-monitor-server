namespace OSRSFriendMonitor.Activity.Models;

public sealed record ItemDrop(
    int ItemId,
    int AccountHash,
    long Timestamp
): ActivityUpdate(AccountHash, Timestamp);
