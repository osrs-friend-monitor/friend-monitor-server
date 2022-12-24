using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OSRSFriendMonitor.Shared.Services.Database.Models;

public sealed record FriendEntry(
    string DisplayName,
    string? PreviousName,
    string? AccountHash,
    DateTime LastUpdated
);

public sealed record RunescapeAccount(
    [property: JsonPropertyName("id")] string AccountHash,
    string UserId,
    string DisplayName,
    string? PreviousName,
    IImmutableList<FriendEntry> Friends
)
{
    public string PartitionKey => AccountHash;

    public static string DisplayNamePath() => "/DisplayName";
}
