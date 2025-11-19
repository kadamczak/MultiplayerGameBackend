namespace MultiplayerGameBackend.Domain.Entities;

public class UserItem
{
    public Guid Id { get; set; }
    
    public required Guid UserId { get; set; }
    public User? User { get; set; }
    
    public required int ItemId { get; set; }
    public Item? Item { get; set; }
    
    public Guid? OfferId { get; set; }
    public UserItemOffer? Offer { get; set; }
    
    // enchants?
}