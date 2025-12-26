using System.ComponentModel.DataAnnotations;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Identity.Requests;

public class LoginDto
{
    [Required]
    [StringLength(User.Constraints.UserNameMaxLength,
        MinimumLength = User.Constraints.UserNameMinLength)]
    public required string UserName { get; set; }
    
    [Required]
    public required string Password { get; set; }
}