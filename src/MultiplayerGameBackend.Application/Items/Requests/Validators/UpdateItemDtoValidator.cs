using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Items.Requests.Validators;

public class UpdateItemDtoValidator : AbstractValidator<UpdateItemDto>
{
    public UpdateItemDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than 0.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(Item.NameMaxLength).WithMessage($"Name cannot exceed {Item.NameMaxLength} characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(Item.DescriptionMaxLength).WithMessage($"Description cannot exceed {Item.DescriptionMaxLength} characters.");
    }
}