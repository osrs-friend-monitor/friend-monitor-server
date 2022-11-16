namespace OSRSFriendMonitor.Activity.Models;

public sealed record LevelUp(
    int Skill, 
    int Level, 
    string AccountHash, 
    string Id, 
    long Timestamp
) : ActivityUpdate(AccountHash, Id, Timestamp);
