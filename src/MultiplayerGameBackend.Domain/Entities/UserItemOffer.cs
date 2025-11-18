namespace MultiplayerGameBackend.Domain.Entities;

public class UserItemOffer
{
    public Guid Id { get; set; }
    public Guid UserItemId { get; set; }
    public UserItem? UserItem { get; set; }
    
    public int Price { get; set; }
}