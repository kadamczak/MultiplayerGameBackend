using MultiplayerGameBackend.Application.Items.Responses;

namespace MultiplayerGameBackend.Application.UserItems.Responses;

public class ReadUserItemDto
{
    public Guid Id { get; set; }
    public required ReadItemDto Item { get; set; }
    public Guid? ActiveOfferId { get; set; }
    public int? ActiveOfferPrice { get; set; }
    
    // enchants?
}