using System.ComponentModel.DataAnnotations;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Users.Requests;

public class UpdateUserCustomizationDto
{
    [StringLength(UserCustomization.Constraints.ColorMaxLength, MinimumLength = UserCustomization.Constraints.ColorMinLength)]
    public required string BodyColor { get; set; } = UserCustomization.Constraints.ColorDefault;
    
    [StringLength(UserCustomization.Constraints.ColorMaxLength, MinimumLength = UserCustomization.Constraints.ColorMinLength)]
    public required string EyeColor { get; set; } = UserCustomization.Constraints.ColorDefault;
    
    [StringLength(UserCustomization.Constraints.ColorMaxLength, MinimumLength = UserCustomization.Constraints.ColorMinLength)]
    public required string WingColor { get; set; } = UserCustomization.Constraints.ColorDefault;
    
    [StringLength(UserCustomization.Constraints.ColorMaxLength, MinimumLength = UserCustomization.Constraints.ColorMinLength)]
    public required string HornColor { get; set; } = UserCustomization.Constraints.ColorDefault;
    
    [StringLength(UserCustomization.Constraints.ColorMaxLength, MinimumLength = UserCustomization.Constraints.ColorMinLength)]
    public required string MarkingsColor { get; set; } = UserCustomization.Constraints.ColorDefault;
    
    public int WingType { get; set; }
    public int HornType { get; set; }
    public int MarkingsType { get; set;}
}