using System.ComponentModel.DataAnnotations;

namespace MultiplayerGameBackend.Application.Identity.Requests;

public class ConfirmEmailDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public required string Email { get; set; }
    
    [Required(ErrorMessage = "Confirmation token is required.")]
    public required string Token { get; set; }
}
