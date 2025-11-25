using MultiplayerGameBackend.Application.UserItems.Responses;

namespace MultiplayerGameBackend.Application.UserItemOffers.Responses;

public class ReadUserItemOfferDto
{
    public Guid Id { get; set; }
    public required ReadUserItemDto UserItem { get; set; }
    public int Price { get; set; }
    
    public Guid SellerId { get; set; }
    public required string SellerUsername { get; set; }
    public DateTime PublishedAt { get; set; }
    
    public Guid? BuyerId { get; set; }
    public string? BuyerUsername { get; set; }
    public DateTime? BoughtAt { get; set; }
}