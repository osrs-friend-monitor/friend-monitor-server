using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace OSRSFriendMonitor.Shared.Services.Database.Models;

public sealed record InGameFriendsList(
    [property: JsonPropertyName("id")] string DisplayName,
    long AccountHash,
    IImmutableSet<string> FriendDisplayNames
);

public sealed record ValidatedFriendsList(
    long AccountHash,
    IImmutableSet<ValidatedFriend> Friends
)
{
    [property: JsonPropertyName("id")]
    string Id => AccountHash.ToString();
}

public sealed record ValidatedFriend(
    string DisplayName,
    long? AccountHash,
    DateTime LastUpdated
)
{
    public override int GetHashCode()
    {
        return DisplayName.GetHashCode();
    }
}

public sealed record RunescapeAccount(
    long AccountHash,
    string UserId,
    string DisplayName
)
{
    [JsonPropertyName("id")]
    public string Id => AccountHash.ToString();

    public static string DisplayNamePath() => "/DisplayName";
}
