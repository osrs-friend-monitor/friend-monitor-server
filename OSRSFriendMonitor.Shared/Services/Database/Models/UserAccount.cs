using System.Text.Json.Serialization;

namespace OSRSFriendMonitor.Shared.Services.Database.Models;

/// <summary>
/// Represents a user account in our system. Stored in the database.
/// </summary>
/// <param name="Id">The account's unique identifier. Comes from Azure AD. Partition key</param>
public sealed record UserAccount(
    [property: JsonPropertyName("id")] string Id
)
{
    public string PartitionKey => Id;
}
