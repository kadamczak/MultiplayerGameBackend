using System.ComponentModel.DataAnnotations;

namespace MultiplayerGameBackend.Domain.Entities;

public class Item
{
    public static class Constraints
    {
        public const int NameMinLength = 2;
        public const int NameMaxLength = 50;
        public const int DescriptionMinLength = 1;
        public const int DescriptionMaxLength = 256;
    }
   
    public int Id { get; set; }
    
    // unique
    [StringLength(Constraints.NameMaxLength, MinimumLength = Constraints.NameMinLength)]
    public required string Name { get; set; }
    
    [StringLength(Constraints.DescriptionMaxLength, MinimumLength = Constraints.DescriptionMinLength)]
    public required string Description { get; set; }
    
    public List<UserItem> UserItems { get; set; } = [];
}