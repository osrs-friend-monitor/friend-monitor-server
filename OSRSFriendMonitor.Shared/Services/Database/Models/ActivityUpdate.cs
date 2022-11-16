using System.Text.Json.Serialization;
using System.Text.Json;

namespace OSRSFriendMonitor.Shared.Services.Database.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(LocationUpdate), "LOCATION")]
[JsonDerivedType(typeof(PlayerDeath), "PLAYER_DEATH")]
public abstract record ActivityUpdate(
    RunescapeAccountIdentifier AccountIdentifier,
    [property: JsonPropertyName("id")] string Id,
    DateTime Time
) {
    public string PartitionKey =>
        $"{AccountIdentifier.CombinedIdentifier()}-{Time.ToUniversalTime().ToString("yyyy-MM")}";
}

 public sealed record LocationUpdate(
    int X,
    int Y,
    int Plane,
    string Id,
    int World,
    RunescapeAccountIdentifier AccountIdentifier,
    DateTime Time
): ActivityUpdate(AccountIdentifier, Id, Time);

public sealed record PlayerDeath(
    int X,
    int Y,
    int Plane,
    string Id,
    int World,
    RunescapeAccountIdentifier AccountIdentifier,
    DateTime Time
): ActivityUpdate(AccountIdentifier, Id, Time);