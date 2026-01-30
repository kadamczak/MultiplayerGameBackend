using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Messages.Requests;
using MultiplayerGameBackend.Application.Messages.Responses;

namespace MultiplayerGameBackend.Application.Messages;

public interface IMessageService
{
    Task<ReadMessageDto> SendMessage(Guid currentUserId, SendMessageDto dto, CancellationToken cancellationToken);
    Task<PagedResult<ReadMessageDto>> GetConversation(Guid currentUserId, GetConversationDto dto, CancellationToken cancellationToken);
    Task<List<ConversationPreviewDto>> GetConversations(Guid currentUserId, CancellationToken cancellationToken);
    Task MarkMessagesAsRead(Guid currentUserId, Guid otherUserId, CancellationToken cancellationToken);
    Task<int> GetUnreadCount(Guid currentUserId, CancellationToken cancellationToken);
}

