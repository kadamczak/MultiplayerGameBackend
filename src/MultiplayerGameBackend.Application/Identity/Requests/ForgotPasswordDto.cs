using System.ComponentModel.DataAnnotations;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Identity.Requests;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(User.Constraints.EmailMaxLength,
        ErrorMessage = "Email cannot exceed {1} characters.")]
    public required string Email { get; set; }
}

