namespace OSRSFriendMonitor.Activity.Models;

public sealed record BossKillCount(
    int NpcId,
    int Count,
    long AccountHash,
    long Timestamp
): ActivityUpdate(AccountHash, Timestamp);
