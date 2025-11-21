using System.ComponentModel.DataAnnotations;

namespace MultiplayerGameBackend.Application.Identity.Requests;

public class DeleteAccountDto
{
    [Required]
    public required string Password { get; set; }
}

