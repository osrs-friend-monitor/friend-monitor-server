using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace OSRSFriendMonitor.Services.Database.Models;

/// <summary>
/// Represents a user account in our system. Stored in the database.
/// </summary>
/// <param name="Id">The account's unique identifier. Comes from Google. Partition key</param>
/// <param name="FirstName">User's first name</param>
/// <param name="LastName">User's last name</param>
/// <param name="Email">User's email address</param>
/// <param name="Friends">List of the user's friends. Each element is another account ID.</param>
/// <param name="FriendRequests">List of the user's incoming friend requests. Each element is another account ID</param>
/// <param name="RunescapeAccounts">List of the user's Runescape accounts. Each element is an account hash</param>
public sealed record UserAccount(
    string Id,
    IImmutableList<string> Friends,
    IImmutableList<string> FriendRequests,
    IImmutableList<RunescapeAccount> RunescapeAccounts
)
{
    public static UserAccount Create(string id)
    {
        return new(
            id,
            ImmutableList.Create<string>(),
            ImmutableList.Create<string>(),
            ImmutableList.Create<RunescapeAccount>()
        );
    }

    /// <summary>
    /// Gets the runescape account path for the given index
    /// </summary>
    /// <param name="index">the index you'd like to patch, or null if you just want to append</param>
    /// <returns>the path to use for the patch operation</returns>
    public static string RunescapeAccountPath(int? index)
    {
        return $"/runescapeAccounts/{index?.ToString() ?? "-"}";
    }
}

public sealed record RunescapeAccount(
    string AccountHash,
    string DisplayName
);


