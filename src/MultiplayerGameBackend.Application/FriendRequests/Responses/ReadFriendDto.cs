namespace MultiplayerGameBackend.Application.FriendRequests.Responses;

public class ReadFriendDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime FriendsSince { get; set; }
}

