namespace OSRSFriendMonitor.Activity.Models;

public sealed record LocationUpdate(
    int X,
    int Y,
    int Plane,
    int AccountHash,
    long Timestamp
): ActivityUpdate(AccountHash, Timestamp);