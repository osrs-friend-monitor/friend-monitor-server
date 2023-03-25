namespace OSRSFriendMonitor.Activity.Models;

public sealed record BossKillCount(
    int NpcId,
    int Count,
    long AccountHash,
    string Id,
    long Timestamp
): ActivityUpdate(AccountHash, Id, Timestamp);
