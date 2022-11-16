namespace OSRSFriendMonitor.Activity.Models;

public sealed record BossKillCount(
    int NpcId,
    int Count,
    string AccountHash,
    string Id,
    long Timestamp
): ActivityUpdate(AccountHash, Id, Timestamp);
