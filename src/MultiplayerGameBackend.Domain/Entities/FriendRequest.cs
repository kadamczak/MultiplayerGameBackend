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
}
