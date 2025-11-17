using System.ComponentModel.DataAnnotations;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Users.Requests;

public class UpdateUserCustomizationDto
{
    [StringLength(UserCustomization.Constraints.ColorMaxLength, MinimumLength = UserCustomization.Constraints.ColorMinLength)]
    public required string HeadColor { get; set; }
    
    [StringLength(UserCustomization.Constraints.ColorMaxLength, MinimumLength = UserCustomization.Constraints.ColorMinLength)]
    public required string BodyColor { get; set; }
    
    [StringLength(UserCustomization.Constraints.ColorMaxLength, MinimumLength = UserCustomization.Constraints.ColorMinLength)]
    public required string TailColor { get; set; }
    
    [StringLength(UserCustomization.Constraints.ColorMaxLength, MinimumLength = UserCustomization.Constraints.ColorMinLength)]
    public required string EyeColor { get; set; }
    
    [StringLength(UserCustomization.Constraints.ColorMaxLength, MinimumLength = UserCustomization.Constraints.ColorMinLength)]
    public required string WingColor { get; set; }
    
    [StringLength(UserCustomization.Constraints.ColorMaxLength, MinimumLength = UserCustomization.Constraints.ColorMinLength)]
    public required string HornColor { get; set; }
    
    [StringLength(UserCustomization.Constraints.ColorMaxLength, MinimumLength = UserCustomization.Constraints.ColorMinLength)]
    public required string MarkingsColor { get; set; }
    
    public int HeadType { get; set; }
    public int BodyType { get; set; }
    public int TailType { get; set; }
    public int EyeType { get; set; }
    public int WingType { get; set; }
    public int HornType { get; set; }
    public int MarkingsType { get; set;}
}