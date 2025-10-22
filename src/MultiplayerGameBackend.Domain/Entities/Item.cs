namespace MultiplayerGameBackend.Domain.Entities;

public class Item
{
    public int Id { get; set; }
    
    public const int NameMaxLength = 50;
    public required string Name { get; set; }
    
    public const int DescriptionMaxLength = 256;
    public required string Description { get; set; }

    
    public List<UserItem> UserItems { get; set; } = [];
}