using System.ComponentModel.DataAnnotations;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Messages.Requests;

public class SendMessageDto
{
    [Required(ErrorMessage = LocalizationKeys.Validation.ReceiverIdRequired)]
    public Guid ReceiverId { get; set; }
    
    [Required(ErrorMessage = LocalizationKeys.Validation.Required)]
    [MinLength(Message.Constraints.ContentMinLength, ErrorMessage = LocalizationKeys.Validation.MinLength)]
    [MaxLength(Message.Constraints.ContentMaxLength, ErrorMessage = LocalizationKeys.Validation.MaxLength)]
    public string Content { get; set; } = string.Empty;
}

