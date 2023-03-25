using OSRSFriendMonitor.Shared.Services.Database.Models;
using System.Text.Json.Serialization;

namespace OSRSFriendMonitor.Services.SocketConnection.Messages;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(LocationUpdateMessage), "LOCATION")]
[JsonDerivedType(typeof(FriendDeathMessage), "FRIEND_DEATH")]
[JsonDerivedType(typeof(LevelUpMessage), "LEVEL_UP")]
public abstract record ServerSocketMessage();
public record LocationUpdateMessage(
    FriendLocationUpdate[] Updates
) : ServerSocketMessage();

public record struct FriendLocationUpdate(
    int X,
    int Y,
    int Plane,
    string DisplayName,
    long AccountHash
);

public sealed record FriendDeathMessage(
    int X,
    int Y,
    int Plane,
    string DisplayName,
    long AccountHash
): ServerSocketMessage();

public sealed record LevelUpMessage(
    Skill Skill,
    int Level,
    string DisplayName,
    long AccountHash
): ServerSocketMessage();