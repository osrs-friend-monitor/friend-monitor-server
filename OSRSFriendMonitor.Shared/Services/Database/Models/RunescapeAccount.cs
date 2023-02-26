using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace OSRSFriendMonitor.Shared.Services.Database.Models;

public sealed record InGameFriendsList(
    [property: JsonPropertyName("id")] string DisplayName,
    string AccountHash,
    IImmutableSet<string> FriendDisplayNames
);

public sealed record ValidatedFriendsList(
    [property: JsonPropertyName("id")] string AccountHash,
    IImmutableSet<ValidatedFriend> Friends
);

public sealed record ValidatedFriend(
    string DisplayName,
    string? AccountHash,
    DateTime LastUpdated
)
{
    public override int GetHashCode()
    {
        return DisplayName.GetHashCode();
    }
}

public sealed record RunescapeAccount(
    [property: JsonPropertyName("id")] string AccountHash,
    string UserId,
    string DisplayName
)
{
    public string PartitionKey => AccountHash;

    public static string DisplayNamePath() => "/DisplayName";
}
