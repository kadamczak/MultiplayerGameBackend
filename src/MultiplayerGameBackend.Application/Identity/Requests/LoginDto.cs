using System.ComponentModel.DataAnnotations;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Identity.Requests;

public class LoginDto
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(User.Constraints.UserNameMaxLength,
        MinimumLength = User.Constraints.UserNameMinLength,
        ErrorMessage = "Username must be between {2} and {1} characters long.")]
    public required string UserName { get; set; }
    
    [Required(ErrorMessage = "Password is required.")]
    public required string Password { get; set; }
}