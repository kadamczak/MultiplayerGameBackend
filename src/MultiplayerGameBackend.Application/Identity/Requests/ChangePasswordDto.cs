namespace MultiplayerGameBackend.Application.Identity.Requests;

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string? RefreshToken { get; set; } = null; // only used by Game clients
}

