using MultiplayerGameBackend.Application.Items.Responses;

namespace MultiplayerGameBackend.Application.UserItems.Responses;

public class ReadUserItemSimplifiedDto
{
    public Guid Id { get; set; }
    public required ReadItemDto Item { get; set; }
    // enchants?
    
    public bool HasActiveOffer { get; set; }
}