namespace OSRSFriendMonitor.Activity.Models;

sealed record QuestComplete(
    int QuestId,
    int AccountHash,
    long Timestamp
): ActivityUpdate(AccountHash, Timestamp);