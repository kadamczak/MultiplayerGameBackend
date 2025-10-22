namespace MultiplayerGameBackend.Application.Identity.Responses;

public class TokenResponseDto
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public int ExpiresInSeconds { get; set; }
}