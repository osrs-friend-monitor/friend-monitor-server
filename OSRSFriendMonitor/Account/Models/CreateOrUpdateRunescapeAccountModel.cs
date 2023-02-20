namespace OSRSFriendMonitor.Account.Models;

public sealed record CreateOrUpdateRunescapeAccountModel(
    string AccountHash,
    string DisplayName,
    string? PreviousDisplayName,
    string[] Friends
);