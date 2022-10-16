using System.Text.Json.Serialization;
using System.Text.Json;

namespace OSRSFriendMonitor.Shared.Services.Database.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(LocationUpdate), "LOCATION")]
public abstract record ActivityUpdate(
    [property: JsonPropertyName("id")] RunescapeAccountIdentifier AccountIdentifier,
    DateTime Time
) {
    public string PartitionKey =>
        $"{AccountIdentifier.UserId}-{Time.ToUniversalTime().ToString("yyyy-MM")}";
}

 public sealed record LocationUpdate(
    int X,
    int Y,
    int Plane,
    RunescapeAccountIdentifier AccountIdentifier,
    DateTime Time
): ActivityUpdate(AccountIdentifier, Time);