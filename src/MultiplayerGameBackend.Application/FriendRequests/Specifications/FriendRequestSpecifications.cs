using System.Linq.Expressions;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.FriendRequests.Specifications;

public static class FriendRequestSpecifications
{
    public static Expression<Func<FriendRequest, bool>> AreFriends(Guid userId1, Guid userId2)
    {
        return fr =>
            ((fr.RequesterId == userId1 && fr.ReceiverId == userId2) ||
             (fr.RequesterId == userId2 && fr.ReceiverId == userId1)) &&
            fr.Status == FriendRequestStatuses.Accepted;
    }

    public static Expression<Func<FriendRequest, bool>> HavePendingRequest(Guid userId1, Guid userId2)
    {
        return fr =>
            ((fr.RequesterId == userId1 && fr.ReceiverId == userId2) ||
             (fr.RequesterId == userId2 && fr.ReceiverId == userId1)) &&
            fr.Status == FriendRequestStatuses.Pending;
    }
    
    public static Expression<Func<FriendRequest, bool>> IsFriendshipWithUser(Guid userId)
    {
        return fr => (fr.RequesterId == userId || fr.ReceiverId == userId) && fr.Status == FriendRequestStatuses.Accepted;
    }
    
    public static Expression<Func<FriendRequest, bool>> HasActiveRelationshipWith(Guid userId)
    {
        return fr =>
            (fr.RequesterId == userId || fr.ReceiverId == userId) &&
            (fr.Status == FriendRequestStatuses.Pending || fr.Status == FriendRequestStatuses.Accepted);
    }
    
    public static Expression<Func<FriendRequest, bool>> IsPendingRequestReceivedBy(Guid userId)
    {
        return fr => fr.ReceiverId == userId && fr.Status == FriendRequestStatuses.Pending;
    }
    
    public static Expression<Func<FriendRequest, bool>> IsPendingRequestSentBy(Guid userId)
    {
        return fr => fr.RequesterId == userId && fr.Status == FriendRequestStatuses.Pending;
    }
    
    public static Expression<Func<FriendRequest, bool>> SearchByRequesterUsername(string searchPhraseLower)
    {
        return fr => fr.Requester.UserName!.ToLower().Contains(searchPhraseLower);
    }
    
    public static Expression<Func<FriendRequest, bool>> SearchByReceiverUsername(string searchPhraseLower)
    {
        return fr => fr.Receiver.UserName!.ToLower().Contains(searchPhraseLower);
    }
    
    public static Expression<Func<FriendRequest, bool>> SearchByOtherUserName(string searchPhraseLower, Guid currentUserId)
    {
        return fr =>
            (fr.RequesterId == currentUserId && fr.Receiver.UserName!.ToLower().Contains(searchPhraseLower)) ||
            (fr.ReceiverId == currentUserId && fr.Requester.UserName!.ToLower().Contains(searchPhraseLower));
    }
    
    public static Expression<Func<FriendRequest, object>> GetOtherUserName(Guid currentUserId)
    {
        return fr => fr.RequesterId == currentUserId ? fr.Receiver.UserName! : fr.Requester.UserName!;
    }
    
    public static Expression<Func<FriendRequest, Guid>> GetOtherUserId(Guid currentUserId)
    {
        return fr => fr.RequesterId == currentUserId ? fr.ReceiverId : fr.RequesterId;
    }
}

