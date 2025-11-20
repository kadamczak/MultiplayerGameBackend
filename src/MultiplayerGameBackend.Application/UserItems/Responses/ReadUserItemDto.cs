using MultiplayerGameBackend.Application.Items.Responses;

namespace MultiplayerGameBackend.Application.UserItems.Responses;

public class ReadUserItemDto
{
    public Guid Id { get; set; }
    public required ReadItemDto Item { get; set; }
    // enchants?
    
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    
    public bool HasActiveOffer { get; set; }
}