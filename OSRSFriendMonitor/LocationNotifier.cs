using OSRSFriendMonitor.Shared.Services.Activity;
using OSRSFriendMonitor.Shared.Services.Database.Models;

namespace OSRSFriendMonitor;

// 1. get account identifiers of everybody who is online
// 2. fetch runescape accounts for those people
// 3. fetch location updates for those people + all friends
// 4. use location updates to get set of 'online people'
// 5. get display names of all 'online people' (should already have them all from step 2)
// 6. go back through and build socket messages for each person
// 7. send messages to each person


sealed record FriendLocationGroup(
	RunescapeAccountIdentifier AccountIdentifier,
	IEnumerable<CachedLocationUpdate> onlineFriendLocations
);

public class OnlineFriendLocationGrouper
{
	public static void Group(IDictionary<RunescapeAccountIdentifier, IEnumerable<RunescapeAccountIdentifier>> onlineAccountsAndFriends,
		IDictionary<RunescapeAccountIdentifier, CachedLocationUpdate> locations)
	{
		IList<FriendLocationGroup> groups = new List<FriendLocationGroup>(onlineAccountsAndFriends.Count);

		foreach (var pair in onlineAccountsAndFriends)
		{
			IList<CachedLocationUpdate> onlineFriendLocations = new List<CachedLocationUpdate>();


			foreach (var friend in pair.Value)
			{
				bool exists = locations.TryGetValue(friend, out var friendLocation);
			}
		}
	}
}

public class LocationNotifier
{
	public LocationNotifier(IList<string> connectedAccounts)
	{
	}
}

