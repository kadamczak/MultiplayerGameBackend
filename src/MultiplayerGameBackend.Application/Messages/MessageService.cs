using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Common.Mappings;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Messages.Requests;
using MultiplayerGameBackend.Application.Messages.Responses;
using MultiplayerGameBackend.Application.Messages.Specifications;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.Application.Messages;

public class MessageService(
    ILogger<MessageService> logger,
    IMultiplayerGameDbContext dbContext,
    MessageMapper messageMapper,
    ILocalizationService localizationService) : IMessageService
{
    public async Task<ReadMessageDto> SendMessage(Guid currentUserId, SendMessageDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {CurrentUserId} is sending message to {ReceiverId}", currentUserId, dto.ReceiverId);

        if (currentUserId == dto.ReceiverId)
            throw new BadRequest(localizationService.GetString(LocalizationKeys.Errors.CannotSendMessageToYourself));

        _ = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == dto.ReceiverId, cancellationToken)
            ?? throw new NotFoundException(localizationService.GetString(LocalizationKeys.Errors.UserNotFound));

        var message = new Message
        {
            SenderId = currentUserId,
            ReceiverId = dto.ReceiverId,
            Content = dto.Content,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Message {MessageId} sent successfully", message.Id);
        return messageMapper.MapToReadMessageDto(message);
    }

    public async Task<PagedResult<ReadMessageDto>> GetConversation(Guid currentUserId, GetConversationDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {CurrentUserId} is fetching conversation with {OtherUserId}", currentUserId, dto.OtherUserId);

        // Verify other user exists
        var otherUserExists = await dbContext.Users.AnyAsync(u => u.Id == dto.OtherUserId, cancellationToken);
        if (!otherUserExists)
            throw new NotFoundException(localizationService.GetString(LocalizationKeys.Errors.UserNotFound));

        // Build count query
        var countQuery = dbContext.Messages
            .AsNoTracking()
            .Where(MessageSpecifications.BetweenUsers(currentUserId, dto.OtherUserId));

        var totalCount = await countQuery.CountAsync(cancellationToken);

        // Build data query with includes and ordering
        var messages = await dbContext.Messages
            .AsNoTracking()
            .Where(MessageSpecifications.BetweenUsers(currentUserId, dto.OtherUserId))
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .OrderByDescending(m => m.SentAt)
            .Skip((dto.PagedQuery.PageNumber - 1) * dto.PagedQuery.PageSize)
            .Take(dto.PagedQuery.PageSize)
            .ToListAsync(cancellationToken);

        var mappedMessages = messages.Select(messageMapper.MapToReadMessageDto).ToList();

        return new PagedResult<ReadMessageDto>(
            mappedMessages,
            totalCount,
            dto.PagedQuery.PageSize,
            dto.PagedQuery.PageNumber);
    }

    public async Task<List<ConversationPreviewDto>> GetConversations(Guid currentUserId, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {CurrentUserId} is fetching conversation list", currentUserId);

        // Get all users that the current user has exchanged messages with
        var conversationUserIds = await dbContext.Messages
            .AsNoTracking()
            .Where(MessageSpecifications.ContainsUser(currentUserId))
            .Select(MessageSpecifications.GetOtherUserId(currentUserId))
            .Distinct()
            .ToListAsync(cancellationToken);

        var conversations = new List<ConversationPreviewDto>();

        foreach (var otherUserId in conversationUserIds)
        {
            var otherUser = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == otherUserId, cancellationToken);

            if (otherUser is null)
                continue;

            var lastMessage = await dbContext.Messages
                .AsNoTracking()
                .Where(MessageSpecifications.BetweenUsers(otherUserId, currentUserId))
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync(cancellationToken);

            var unreadCount = await dbContext.Messages
                .AsNoTracking()
                .CountAsync(MessageSpecifications.UnreadBetweenUsers(otherUserId, currentUserId),
                    cancellationToken);

            conversations.Add(new ConversationPreviewDto
            {
                OtherUserId = otherUserId,
                OtherUsername = otherUser.UserName!,
                OtherProfilePictureUrl = otherUser.ProfilePictureUrl,
                LastMessageContent = lastMessage?.Content,
                LastMessageSentAt = lastMessage?.SentAt,
                UnreadCount = unreadCount
            });
        }

        return conversations.OrderByDescending(c => c.LastMessageSentAt).ToList();
    }

    public async Task MarkMessagesAsRead(Guid currentUserId, Guid otherUserId, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {CurrentUserId} is marking messages from {OtherUserId} as read", currentUserId, otherUserId);

        var unreadMessages = await dbContext.Messages
            .Where(MessageSpecifications.UnreadBetweenUsers(otherUserId, currentUserId))
            .ToListAsync(cancellationToken);

        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Marked {Count} messages as read", unreadMessages.Count);
    }

    public async Task<int> GetUnreadCount(Guid currentUserId, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {CurrentUserId} is fetching unread message count", currentUserId);

        var unreadCount = await dbContext.Messages
            .AsNoTracking()
            .CountAsync(MessageSpecifications.UnreadByUser(currentUserId), cancellationToken);

        return unreadCount;
    }
}

