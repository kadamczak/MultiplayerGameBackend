using FluentValidation;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Identity.Requests.Validators;

public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .Length(User.Constraints.RawPasswordMinLength, User.Constraints.RawPasswordMaxLength)
            .WithMessage($"New password must be between {User.Constraints.RawPasswordMinLength} and {User.Constraints.RawPasswordMaxLength} characters long.")
            .Matches(@"[A-Z]").WithMessage("New password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("New password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("New password must contain at least one digit.");
    }
}

