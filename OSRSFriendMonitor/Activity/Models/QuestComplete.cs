namespace OSRSFriendMonitor.Activity.Models;

sealed record QuestComplete(
    int QuestId,
    long AccountHash,
    long Timestamp
): ActivityUpdate(AccountHash, Timestamp);