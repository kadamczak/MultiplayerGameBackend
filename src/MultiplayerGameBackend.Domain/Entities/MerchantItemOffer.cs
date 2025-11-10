namespace MultiplayerGameBackend.Domain.Entities;

public class MerchantItemOffer
{
    public int Id { get; set; }
    
    public int MerchantId { get; set; }
    public InGameMerchant? Merchant { get; set; }
    
    public int ItemId { get; set; }
    public Item? Item { get; set; }
    
    public int Price { get; set; }
}