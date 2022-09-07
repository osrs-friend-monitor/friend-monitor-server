namespace OSRSFriendMonitor.Activity.Models;

public sealed record LevelUp(
    int Skill,
    int Level,
    int AccountHash,
   long Timestamp
): ActivityUpdate(AccountHash, Timestamp);
