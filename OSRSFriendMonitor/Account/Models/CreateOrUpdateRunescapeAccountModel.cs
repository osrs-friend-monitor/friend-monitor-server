namespace OSRSFriendMonitor.Account.Models;

public sealed record UnconfirmedFriend(
    string DisplayName,
    string? PreviousDisplayName
);

public sealed record CreateOrUpdateRunescapeAccountModel(
    string AccountHash,
    string DisplayName,
    string? PreviousDisplayName,
    UnconfirmedFriend[] Friends
);