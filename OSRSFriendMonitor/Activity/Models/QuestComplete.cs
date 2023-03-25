namespace OSRSFriendMonitor.Activity.Models;

sealed record QuestComplete(
    int QuestId,
    long AccountHash,
    string Id,
    long Timestamp
): ActivityUpdate(AccountHash, Id, Timestamp);