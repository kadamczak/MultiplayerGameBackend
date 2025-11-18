namespace MultiplayerGameBackend.Domain.Entities;

public class UserItemOffer
{
    public static class Constraints
    {
        public const int MinPrice = 0;
        public const int MaxPrice = 999_999;
    }
    
    public Guid Id { get; set; }
    public Guid UserItemId { get; set; }
    public UserItem? UserItem { get; set; }
    
    public int Price { get; set; }
}