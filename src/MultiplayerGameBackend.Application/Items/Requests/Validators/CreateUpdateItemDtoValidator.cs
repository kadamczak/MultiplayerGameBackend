using FluentValidation;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Items.Requests.Validators;

public class CreateUpdateItemDtoValidator : AbstractValidator<CreateUpdateItemDto>
{
    public  CreateUpdateItemDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Length(Item.Constraints.NameMinLength, Item.Constraints.NameMaxLength)
            .WithMessage($"Name must be between {Item.Constraints.NameMinLength} and {Item.Constraints.NameMaxLength} characters long.");
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .Length(Item.Constraints.DescriptionMinLength, Item.Constraints.DescriptionMaxLength)
            .WithMessage($"Description must be between {Item.Constraints.DescriptionMinLength} and {Item.Constraints.DescriptionMaxLength} characters long.");
        
        RuleFor(x => x.ThumbnailUrl)
            .NotEmpty().WithMessage("Thumbnail URL is required.")
            .MaximumLength(Item.Constraints.ThumbnailUrlMaxLength)
            .WithMessage($"Thumbnail URL can be maximally {Item.Constraints.ThumbnailUrlMaxLength} characters long.");
        
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Type is required.")
            .Must(ItemTypes.IsValidItemType)
            .WithMessage($"Item must be one of the following: {string.Join(", ", ItemTypes.AllItemTypes)}");
    }
}