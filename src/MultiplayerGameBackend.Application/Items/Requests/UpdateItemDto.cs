using System.ComponentModel.DataAnnotations;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Items.Requests;

public class UpdateItemDto
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(Item.Constraints.NameMaxLength,
        MinimumLength = Item.Constraints.NameMinLength,
        ErrorMessage = "Name must be between {2} and {1} characters.")]
    public required string Name { get; set; }
    
    [Required(ErrorMessage = "Description is required.")]
    [StringLength(Item.Constraints.DescriptionMaxLength,
        MinimumLength = Item.Constraints.DescriptionMinLength,
        ErrorMessage = "Description must be between {2} and {1} characters.")]
    public required string Description { get; set; }
    
    [Required(ErrorMessage = "Type is required.")]
    [StringLength(Item.Constraints.TypeMaxLength,
        ErrorMessage = "Description must be between {2} and {1} characters.")]
    public required string Type { get; set; } // todo: check if valid
}