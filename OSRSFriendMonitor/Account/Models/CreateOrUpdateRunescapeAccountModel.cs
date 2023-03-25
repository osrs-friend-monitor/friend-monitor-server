namespace OSRSFriendMonitor.Account.Models;

public sealed record CreateOrUpdateRunescapeAccountModel(
    long AccountHash,
    string DisplayName,
    string? PreviousDisplayName,
    string[] Friends
);