using System.ComponentModel.DataAnnotations;

namespace MultiplayerGameBackend.Domain.Entities;

public class UserCustomization
{
    public static class Constraints
    {
        public const int ColorMinLength = 7;
        public const int ColorMaxLength = 9;
        public const string ColorDefault = "#CCA878";
    }
    
    public int Id { get; set; }
    
    [StringLength(Constraints.ColorMaxLength, MinimumLength = Constraints.ColorMinLength)]
    public required string BodyColor { get; set; } = Constraints.ColorDefault;
    
    [StringLength(Constraints.ColorMaxLength, MinimumLength = Constraints.ColorMinLength)]
    public required string EyeColor { get; set; } = Constraints.ColorDefault;
    
    [StringLength(Constraints.ColorMaxLength, MinimumLength = Constraints.ColorMinLength)]
    public required string WingColor { get; set; } = Constraints.ColorDefault;
    
    [StringLength(Constraints.ColorMaxLength, MinimumLength = Constraints.ColorMinLength)]
    public required string HornColor { get; set; } = Constraints.ColorDefault;
    
    [StringLength(Constraints.ColorMaxLength, MinimumLength = Constraints.ColorMinLength)]
    public required string MarkingsColor { get; set; } = Constraints.ColorDefault;
    
    public int WingType { get; set; }
    public int HornType { get; set; }
    public int MarkingsType { get; set;}
    
    public Guid UserId { get; set;  }
    public User? User { get; set; }
}