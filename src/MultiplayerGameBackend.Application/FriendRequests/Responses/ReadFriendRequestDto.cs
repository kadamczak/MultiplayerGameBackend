namespace MultiplayerGameBackend.Application.FriendRequests.Responses;

public class ReadFriendRequestDto
{
    public Guid Id { get; set; }
    
    public Guid RequesterId { get; set; }
    public string RequesterUserName { get; set; } = string.Empty;
    public string? RequesterProfilePictureUrl { get; set; }
    
    public Guid ReceiverId { get; set; }
    public string ReceiverUserName { get; set; } = string.Empty;
    public string? ReceiverProfilePictureUrl { get; set; }
    
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

