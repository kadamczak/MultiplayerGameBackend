using MultiplayerGameBackend.Application.Items.Responses;

namespace MultiplayerGameBackend.Application.MerchantItemOffers.Responses;

public class ReadMerchantOfferDto
{
    public int Id { get; set; }
    public required ReadItemDto Item { get; set; }
    public int Price { get; set; }
}