using System.ComponentModel.DataAnnotations;
using MultiplayerGameBackend.Application.Common;

namespace MultiplayerGameBackend.Application.Messages.Requests;

public class GetConversationDto
{
    [Required(ErrorMessage = LocalizationKeys.Validation.Required)]
    public Guid OtherUserId { get; set; }
    
    public PagedQuery PagedQuery { get; set; } = new();
}

