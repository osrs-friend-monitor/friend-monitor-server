namespace OSRSFriendMonitor.Activity.Models;

sealed record QuestComplete(
    int QuestId,
    string AccountHash,
    string Id,
    long Timestamp
): ActivityUpdate(AccountHash, Id, Timestamp);