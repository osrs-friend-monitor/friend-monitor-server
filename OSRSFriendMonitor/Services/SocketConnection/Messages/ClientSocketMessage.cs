using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace OSRSFriendMonitor.Services.SocketConnection.Messages;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LocationUpdateSpeed
{
    [EnumMember(Value = "SLOW")]
    SLOW,
    [EnumMember(Value = "FAST")]
    FAST
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(LocationUpdateSpeedMessage), "LOCATION_UPDATE_SPEED")]
public abstract record ClientSocketMessage();

public record LocationUpdateSpeedMessage(
    LocationUpdateSpeed Speed
) : ClientSocketMessage();