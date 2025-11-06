namespace MultiplayerGameBackend.Application.Users.Responses;

public class UserGameInfoDto
{
    public Guid Id { get; set; }
    public required string UserName { get; set; }
    public int Balance { get; set; }
}