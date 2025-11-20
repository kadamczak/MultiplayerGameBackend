using MultiplayerGameBackend.Application.UserItems.Responses;

namespace MultiplayerGameBackend.Application.UserItemOffers.Responses;

public class ReadActiveUserItemOfferDto
{
    public Guid Id { get; set; }
    public required ReadUserItemDto UserItem { get; set; }
    public int Price { get; set; }
}