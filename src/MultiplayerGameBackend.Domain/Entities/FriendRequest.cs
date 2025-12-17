using System.Linq.Expressions;
using MultiplayerGameBackend.Domain.Constants;

namespace MultiplayerGameBackend.Domain.Entities;

public class FriendRequest
{
    public static class Constraints
    {
        public const int MaxPendingRequestsPerUser = 100;
        public const int MaxFriendsPerUser = 500;
    }
    
    public Guid Id { get; set; }
    
    public Guid RequesterId { get; set; }
    public User Requester { get; set; } = null!;
    
    public Guid ReceiverId { get; set; }
    public User Receiver { get; set; } = null!;
    
    public string Status { get; set; } = FriendRequestStatuses.Pending;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? RespondedAt { get; set; }
    
    public static Expression<Func<FriendRequest, bool>> AreFriends(Guid userId1, Guid userId2)
    {
        return fr =>
            ((fr.RequesterId == userId1 && fr.ReceiverId == userId2) ||
             (fr.RequesterId == userId2 && fr.ReceiverId == userId1)) &&
            fr.Status == FriendRequestStatuses.Accepted;
    }

    public static Expression<Func<FriendRequest, bool>> HasPendingRequest(Guid userId1, Guid userId2)
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
}
