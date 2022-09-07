namespace OSRSFriendMonitor.Activity.Models;

public sealed record BossKillCount(
    int NpcId,
    int Count,
    int AccountHash,
    long Timestamp
): ActivityUpdate(AccountHash, Timestamp);
