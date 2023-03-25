using System.Text.Json.Serialization;
using System.Text.Json;

namespace OSRSFriendMonitor.Shared.Services.Database.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(LocationUpdate), "LOCATION")]
[JsonDerivedType(typeof(PlayerDeath), "PLAYER_DEATH")]
[JsonDerivedType(typeof(LevelUp), "LEVEL_UP")]
public abstract record ActivityUpdate(
    long AccountHash,
    [property: JsonPropertyName("id")] string Id,
    DateTime Time
) {
    public string PartitionKey =>
        $"{AccountHash}-{Time.ToUniversalTime().ToString("yyyy-MM")}";
}

 public sealed record LocationUpdate(
    int X,
    int Y,
    int Plane,
    string Id,
    int World,
    long AccountHash,
    DateTime Time
): ActivityUpdate(AccountHash, Id, Time);

public sealed record PlayerDeath(
    int X,
    int Y,
    int Plane,
    string Id,
    int World,
    long AccountHash,
    DateTime Time
): ActivityUpdate(AccountHash, Id, Time);

public sealed record LevelUp(
    Skill Skill,
    int Level,
    string Id,
    long AccountHash,
    DateTime Time
): ActivityUpdate(AccountHash, Id, Time);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Skill
{
    Attack,
    Defence,
    Strength,
    Hitpoints,
    Ranged,
    Prayer,
    Magic,
    Cooking,
    Woodcutting,
    Fletching,
    Fishing,
    Firemaking,
    Crafting,
    Smithing,
    Mining,
    Herblore,
    Agility,
    Thieving,
    Slayer,
    Farming,
    Runecraft,
    Hunter,
    Construction,
    Overall
}