namespace MultiplayerGameBackend.Domain.Entities;

public class Item
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }

    
    public List<UserItem> UserItems { get; set; } = [];
}