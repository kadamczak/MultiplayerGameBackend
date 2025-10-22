using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace MultiplayerGameBackend.Application.Items.Requests.Validators;

public class UpdateItemDtoValidator : AbstractValidator<UpdateItemDto>
{
    private const int MaxNameLength = 50;
    private const int MaxDescriptionLength = 256;
    
    public UpdateItemDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than 0.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(MaxNameLength).WithMessage($"Name cannot exceed {MaxNameLength} characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(MaxDescriptionLength).WithMessage($"Description cannot exceed {MaxDescriptionLength} characters.");
    }
}