using System.ComponentModel.DataAnnotations;

namespace MultiplayerGameBackend.Domain.Entities;

public class UserCustomization
{
    public static class Constraints
    {
        public const int ColorMinLength = 7;
        public const int ColorMaxLength = 9;
    }
    
    public int Id { get; set; }
    
    [StringLength(Constraints.ColorMaxLength, MinimumLength = Constraints.ColorMinLength)]
    public required string HeadColor { get; set; }
    
    [StringLength(Constraints.ColorMaxLength, MinimumLength = Constraints.ColorMinLength)]
    public required string BodyColor { get; set; }
    
    [StringLength(Constraints.ColorMaxLength, MinimumLength = Constraints.ColorMinLength)]
    public required string TailColor { get; set; }
    
    [StringLength(Constraints.ColorMaxLength, MinimumLength = Constraints.ColorMinLength)]
    public required string EyeColor { get; set; }
    
    [StringLength(Constraints.ColorMaxLength, MinimumLength = Constraints.ColorMinLength)]
    public required string WingColor { get; set; }
    
    [StringLength(Constraints.ColorMaxLength, MinimumLength = Constraints.ColorMinLength)]
    public required string HornColor { get; set; }
    
    [StringLength(Constraints.ColorMaxLength, MinimumLength = Constraints.ColorMinLength)]
    public required string MarkingsColor { get; set; }

    public int HeadType { get; set; }
    public int BodyType { get; set; }
    public int TailType { get; set; }
    public int EyeType { get; set; }
    public int WingType { get; set; }
    public int HornType { get; set; }
    public int MarkingsType { get; set;}
    
    public Guid UserId { get; set;  }
    public User? User { get; set; }
}