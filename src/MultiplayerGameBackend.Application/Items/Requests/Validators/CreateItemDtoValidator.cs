using FluentValidation;

namespace MultiplayerGameBackend.Application.Items.Requests.Validators;

public class CreateItemDtoValidator : AbstractValidator<CreateItemDto>
{
    private const int MaxNameLength = 50;
    private const int MaxDescriptionLength = 256;
    
    public CreateItemDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(MaxNameLength).WithMessage($"Name cannot exceed {MaxNameLength} characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(MaxDescriptionLength).WithMessage($"Description cannot exceed {MaxDescriptionLength} characters.");
    }
}