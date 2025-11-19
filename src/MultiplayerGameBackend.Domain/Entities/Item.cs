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
        public const int TypeMaxLength = 20;
        public const int ThumbnailUrlMaxLength = 256;
    }
   
    public int Id { get; set; }
    
    // unique
    [StringLength(Constraints.NameMaxLength, MinimumLength = Constraints.NameMinLength)]
    public required string Name { get; set; }
    
    [StringLength(Constraints.DescriptionMaxLength, MinimumLength = Constraints.DescriptionMinLength)]
    public required string Description { get; set; }
    
    [Required]
    [StringLength(Constraints.TypeMaxLength)]
    public required string Type { get; set; }
    
    [Required]
    [StringLength(Constraints.ThumbnailUrlMaxLength)]
    public required string ThumbnailUrl { get; set; }
    
    public List<UserItem> UserItems { get; set; } = [];
    public List<MerchantItemOffer> MerchantItemOffers { get; set; } = [];
}