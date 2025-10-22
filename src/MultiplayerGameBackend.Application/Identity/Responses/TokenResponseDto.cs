namespace MultiplayerGameBackend.Application.Identity.Responses;

public class TokenResponseDto
{
    public required string AccessToken { get; set; }
    public string? RefreshToken { get; set; } // Null for browser clients
    public int ExpiresInSeconds { get; set; }
}