namespace OSRSFriendMonitor.Activity.Models;

public sealed record LevelUp(
    int Skill,
    int Level,
    long AccountHash,
   long Timestamp
): ActivityUpdate(AccountHash, Timestamp);
