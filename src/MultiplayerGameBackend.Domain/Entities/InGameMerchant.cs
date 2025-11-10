namespace MultiplayerGameBackend.Domain.Entities;

public class InGameMerchant
{
    public int Id { get; set; }
    public List<MerchantItemOffer> ItemOffers { get; set; } = [];
}