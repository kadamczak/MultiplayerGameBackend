namespace MultiplayerGameBackend.Application.Messages.Responses;

public class ReadMessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    
    public string Content { get; set; } = string.Empty;
    
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}

