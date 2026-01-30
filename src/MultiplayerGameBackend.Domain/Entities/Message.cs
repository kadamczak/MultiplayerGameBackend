using System.ComponentModel.DataAnnotations;

namespace MultiplayerGameBackend.Domain.Entities;

public class Message
{
    public static class Constraints
    {
        public const int ContentMaxLength = 1000;
        public const int ContentMinLength = 1;
    }
    
    public Guid Id { get; set; }
    
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;
    
    public Guid ReceiverId { get; set; }
    public User Receiver { get; set; } = null!;
    
    [Required]
    [MaxLength(Constraints.ContentMaxLength)]
    public string Content { get; set; } = string.Empty;
    
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    public bool IsRead { get; set; } = false;
    
    public DateTime? ReadAt { get; set; }
}

