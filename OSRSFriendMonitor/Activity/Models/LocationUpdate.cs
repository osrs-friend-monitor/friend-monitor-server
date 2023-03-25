namespace OSRSFriendMonitor.Activity.Models;

public sealed record LocationUpdate(
    int X, 
    int Y, 
    int Plane, 
    int World, 
    long AccountHash, 
    string Id, 
    long Timestamp
) : ActivityUpdate(AccountHash, Id, Timestamp);