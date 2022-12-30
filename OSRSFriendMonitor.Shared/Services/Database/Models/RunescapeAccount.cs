using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OSRSFriendMonitor.Shared.Services.Database.Models;

public sealed record Friend(
    string DisplayName,
    string? PreviousName,
    string AccountHash,
    bool IsMutual,
    DateTime BecameFriends
);

public sealed record RunescapeAccount(
    [property: JsonPropertyName("id")] string AccountHash,
    string UserId,
    string DisplayName,
    string? PreviousName,
    IImmutableList<Friend> Friends
)
{
    public string PartitionKey => AccountHash;

    public static string DisplayNamePath() => "/DisplayName";
    public static string PreviousDisplayNamePath() => "/PreviousName";
}
