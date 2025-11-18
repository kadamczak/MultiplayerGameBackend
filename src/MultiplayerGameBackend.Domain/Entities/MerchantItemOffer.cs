namespace MultiplayerGameBackend.Domain.Entities;

public class MerchantItemOffer
{
    public static class Constraints
    {
        public const int MinPrice = 0;
        public const int MaxPrice = 999_999;
    }
    
    public int Id { get; set; }
    
    public int MerchantId { get; set; }
    public InGameMerchant? Merchant { get; set; }
    
    public int ItemId { get; set; }
    public Item? Item { get; set; }
    
    public int Price { get; set; }
}