using System.ComponentModel.DataAnnotations;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Identity.Requests;

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    [MaxLength(User.Constraints.EmailMaxLength)]
    public required string Email { get; set; }
}