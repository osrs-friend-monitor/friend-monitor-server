using OSRSFriendMonitor.Shared.Services.Database.Models;

namespace OSRSFriendMonitor.Shared.Services.Account;




//public record struct RunescapeAccountFriendDiffResult(
//    string AccountHash,
//    IList<RunescapeAccountFriendDiffResult.FriendNoLongerPresent> FriendsToRemoveFromConfirmedList,
//    IList<RunescapeAccountFriendDiffResult.MissingFromConfirmedFriends> FriendsToSearchFor
//)
//{

//    public bool HasChanges => FriendsToRemoveFromConfirmedList.Count > 0 || FriendsToSearchFor.Count > 0;

//    public record struct MissingFromConfirmedFriends(
//        string DisplayName
//    );

//    public record struct FriendNoLongerPresent(
//        string AccountHash
//    );

//}
//public static class RunescapeAccountFriendDiffer
//{
//    public static RunescapeAccountFriendDiffResult PerformDiff(RunescapeAccount account, Tuple<string, string?>[] unlinkedFriends)
//    {
//        ISet<string> matchingAccountHashes = new HashSet<string>();
//        ISet<string> displayNamesNotFoundInConfirmedFriends = new HashSet<string>();

//        ISet<string> unlinkedFriendDisplayNames = unlinkedFriends.Select(x => x.Item1).ToHashSet();

//        ISet<string> allConfirmedFriendAccountHashes = new HashSet<string>();

//        foreach (ConfirmedFriend friend in account.Friends)
//        {
//            allConfirmedFriendAccountHashes.Add(friend.AccountHash);

//            if (unlinkedFriendDisplayNames.Contains(friend.DisplayName))
//            {
//                matchingAccountHashes.Add(friend.AccountHash);
//            } 
//            else
//            {
//                displayNamesNotFoundInConfirmedFriends.Add(friend.DisplayName);
//            }
//        }

//        IList<string> noLongerNeededAccountHashes = allConfirmedFriendAccountHashes
//            .Except(matchingAccountHashes)
//            .ToList();

//        IList<RunescapeAccountFriendDiffResult.FriendNoLongerPresent> friendsNoLongerPresent = noLongerNeededAccountHashes
//            .Select(h => new RunescapeAccountFriendDiffResult.FriendNoLongerPresent(h))
//            .ToList();

//        IList<RunescapeAccountFriendDiffResult.MissingFromConfirmedFriends> missingFriends = displayNamesNotFoundInConfirmedFriends
//            .Select(displayName => new RunescapeAccountFriendDiffResult.MissingFromConfirmedFriends(displayName))
//            .ToList();

//        return new(
//            account.AccountHash,
//            FriendsToRemoveFromConfirmedList: friendsNoLongerPresent,
//            FriendsToSearchFor: missingFriends
//        );
//    } 
//}
