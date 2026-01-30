using MultiplayerGameBackend.Application.Messages.Responses;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Common.Mappings;

public class MessageMapper
{
    public ReadMessageDto MapToReadMessageDto(Message message)
    {
        return new ReadMessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            Content = message.Content,
            SentAt = message.SentAt,
            IsRead = message.IsRead,
            ReadAt = message.ReadAt
        };
    }
}

