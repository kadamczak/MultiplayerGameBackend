namespace MultiplayerGameBackend.Application.Friends.Responses;

public class ReadFriendDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime FriendsSince { get; set; }
}

