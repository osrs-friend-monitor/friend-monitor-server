namespace OSRSFriendMonitor;

public class LocationUpdateNotifier
{
    private readonly LiveConnectionManager _connectionManager;

    public LocationUpdateNotifier(LiveConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public async Task NotifyOnlineFriendsOfLocationUpdateAsync(LocationUpdate update)
    {
        List<RunescapeAccountIdentifier> friends = await GetFriendsForAccount(new(update.AccountHash));

        foreach (var friend in friends)
        {
            await _connectionManager.SendMessageToAccountAsync(friend, $"friend {friend.AccountHash} is now at x: {update.X}, y: {update.Y}, plane: {update.Plane}");
        }
    }

    private async Task<List<RunescapeAccountIdentifier>> GetFriendsForAccount(RunescapeAccountIdentifier identifier)
    {
        return new();
    }
}
