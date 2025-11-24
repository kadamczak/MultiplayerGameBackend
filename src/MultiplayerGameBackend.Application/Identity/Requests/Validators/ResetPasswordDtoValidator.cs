using FluentValidation;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Identity.Requests.Validators;

public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(User.Constraints.EmailMaxLength).WithMessage($"Email cannot exceed {User.Constraints.EmailMaxLength} characters.")
            .EmailAddress().WithMessage("Invalid email format.");
        
        RuleFor(x => x.ResetToken)
            .NotEmpty().WithMessage("Reset token is required.");
        
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .Length(User.Constraints.RawPasswordMinLength, User.Constraints.RawPasswordMaxLength)
            .WithMessage($"New password must be between {User.Constraints.RawPasswordMinLength} and {User.Constraints.RawPasswordMaxLength} characters long.")
            .Matches(@"[A-Z]").WithMessage("New password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("New password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("New password must contain at least one digit.");
    }
}