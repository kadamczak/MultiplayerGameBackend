namespace MultiplayerGameBackend.Application.Users.Requests;

public sealed class RegisterDto
{
    public required string UserName { get; set; }
    public required string UserEmail { get; set; }
    public required string Password { get; set; }
}