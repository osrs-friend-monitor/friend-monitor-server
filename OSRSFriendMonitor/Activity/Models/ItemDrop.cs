namespace OSRSFriendMonitor.Activity.Models;

public sealed record ItemDrop(
    int ItemId,
    long AccountHash,
    string Id,
    long Timestamp
): ActivityUpdate(AccountHash, Id, Timestamp);
