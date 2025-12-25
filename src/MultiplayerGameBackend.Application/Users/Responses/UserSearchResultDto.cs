namespace MultiplayerGameBackend.Application.Users.Responses;

public class UserSearchResultDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}

