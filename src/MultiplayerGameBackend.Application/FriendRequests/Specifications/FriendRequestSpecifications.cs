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
    
    public static Expression<Func<FriendRequest, bool>> IsPendingRequestReceivedBy(Guid userId)
    {
        return fr => fr.ReceiverId == userId && fr.Status == FriendRequestStatuses.Pending;
    }
    
    public static Expression<Func<FriendRequest, bool>> IsPendingRequestSentBy(Guid userId)
    {
        return fr => fr.RequesterId == userId && fr.Status == FriendRequestStatuses.Pending;
    }
    
    public static Expression<Func<FriendRequest, bool>> SearchByRequesterUsername(string searchPhrase)
    {
        return fr => fr.Requester.UserName!.ToLower().Contains(searchPhrase);
    }
    
    public static Expression<Func<FriendRequest, bool>> SearchByReceiverUsername(string searchPhrase)
    {
        return fr => fr.Receiver.UserName!.ToLower().Contains(searchPhrase);
    }
}

