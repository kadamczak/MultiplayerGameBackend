using System.Linq.Expressions;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.FriendRequests.Specifications;

public static class FriendRequestSortingSelectors
{
    public static Dictionary<string, Expression<Func<FriendRequest, object>>> ForReceivedRequests()
        => new()
        {
            { nameof(FriendRequest.CreatedAt), fr => fr.CreatedAt },
            { nameof(User.UserName), fr => fr.Requester.UserName! }
        };
    
    public static Dictionary<string, Expression<Func<FriendRequest, object>>> ForSentRequests()
        => new()
        {
            { nameof(FriendRequest.CreatedAt), fr => fr.CreatedAt },
            { nameof(User.UserName), fr => fr.Receiver.UserName! }
        };
    
    public static Dictionary<string, Expression<Func<FriendRequest, object>>> ForFriends(Guid currentUserId)
        => new()
        {
            { nameof(FriendRequest.RespondedAt), fr => fr.RespondedAt ?? fr.CreatedAt },
            { nameof(User.UserName), fr => fr.RequesterId == currentUserId ? fr.Receiver.UserName! : fr.Requester.UserName! }
        };
}