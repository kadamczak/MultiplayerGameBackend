namespace MultiplayerGameBackend.Application.Messages.Responses;

public class ConversationPreviewDto
{
    public Guid OtherUserId { get; set; }
    public string OtherUsername { get; set; } = string.Empty;
    public string? OtherProfilePictureUrl { get; set; }
    public string? LastMessageContent { get; set; }
    public DateTime? LastMessageSentAt { get; set; }
    public int UnreadCount { get; set; }
}

