using System.ComponentModel.DataAnnotations;

namespace MultiplayerGameBackend.Application.Identity.Requests;

public class ConfirmEmailDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
    
    [Required]
    public required string Token { get; set; }
}
