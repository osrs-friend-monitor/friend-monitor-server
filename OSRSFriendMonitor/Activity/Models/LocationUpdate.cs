namespace OSRSFriendMonitor.Activity.Models;

public sealed record LocationUpdate(
    int X,
    int Y,
    int Plane,
    long AccountHash,
    long Timestamp
): ActivityUpdate(AccountHash, Timestamp);