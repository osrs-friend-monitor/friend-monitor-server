namespace OSRSFriendMonitor.Activity.Models;

public sealed record ItemDrop(
    int ItemId,
    long AccountHash,
    long Timestamp
): ActivityUpdate(AccountHash, Timestamp);
