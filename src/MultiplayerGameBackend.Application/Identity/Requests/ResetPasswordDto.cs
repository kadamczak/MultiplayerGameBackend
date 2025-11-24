using System.ComponentModel.DataAnnotations;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Identity.Requests;

public class ResetPasswordDto
{
    public required string Email { get; set; }
    public required string ResetToken { get; set; }
    public required string NewPassword { get; set; }
}

