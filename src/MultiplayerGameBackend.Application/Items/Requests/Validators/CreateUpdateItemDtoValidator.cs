using MultiplayerGameBackend.Application.Common.Validators;
using MultiplayerGameBackend.Application.Common;
using FluentValidation;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Items.Requests.Validators;

public class CreateUpdateItemDtoValidator : AbstractValidator<CreateUpdateItemDto>
{
    public  CreateUpdateItemDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.NameRequired))
            .Length(Item.Constraints.NameMinLength, Item.Constraints.NameMaxLength)
            .WithMessage(ValidatorLocalizer.GetString(
                LocalizationKeys.Validation.NameLength,
                Item.Constraints.NameMinLength,
                Item.Constraints.NameMaxLength));
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.DescriptionRequired))
            .Length(Item.Constraints.DescriptionMinLength, Item.Constraints.DescriptionMaxLength)
            .WithMessage(ValidatorLocalizer.GetString(
                LocalizationKeys.Validation.DescriptionLength,
                Item.Constraints.DescriptionMinLength,
                Item.Constraints.DescriptionMaxLength));
        
        RuleFor(x => x.ThumbnailUrl)
            .NotEmpty().WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.Required, "Thumbnail URL"))
            .MaximumLength(Item.Constraints.ThumbnailUrlMaxLength)
            .WithMessage(ValidatorLocalizer.GetString(
                LocalizationKeys.Validation.MaxLength,
                "Thumbnail URL",
                Item.Constraints.ThumbnailUrlMaxLength));
        
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.TypeRequired))
            .Must(ItemTypes.IsValidItemType)
            .WithMessage(ValidatorLocalizer.GetString(
                LocalizationKeys.Validation.InvalidValue,
                string.Join(", ", ItemTypes.AllItemTypes)));
    }
}