namespace OSRSFriendMonitor.Shared.Services.Account.Queue.Messages;

public sealed record PotentialFriendRemoval(
    string SendingAccountHash,
    string ReceivingAccountHash,
    DateTime Time
);