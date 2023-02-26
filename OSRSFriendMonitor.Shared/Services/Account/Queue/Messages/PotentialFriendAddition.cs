namespace OSRSFriendMonitor.Shared.Services.Account.Queue.Messages;

public sealed record PotentialFriendAddition(
    string SendingAccountHash,
    string ReceivingAccountHash,
    DateTime Time
);