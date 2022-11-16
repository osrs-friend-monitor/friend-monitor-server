using System.Text.Json.Serialization;

namespace OSRSFriendMonitor.Services.SocketConnection.Messages;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(LocationUpdateMessage), "LOCATION")]
[JsonDerivedType(typeof(FriendDeathMessage), "FRIEND_DEATH")]
public abstract record ServerSocketMessage();
public record LocationUpdateMessage(
    FriendLocationUpdate[] Updates
) : ServerSocketMessage();

public record struct FriendLocationUpdate(
    int X,
    int Y,
    int Plane,
    string DisplayName,
    string AccountHash
);

public sealed record FriendDeathMessage(
    int X,
    int Y,
    int Plane,
    string DisplayName,
    string AccountHash
): ServerSocketMessage();