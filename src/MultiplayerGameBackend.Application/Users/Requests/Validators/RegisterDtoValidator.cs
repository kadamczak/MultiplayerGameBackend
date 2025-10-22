using FluentValidation;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Users.Requests.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(User.UserNameMaxLength).WithMessage($"Username cannot exceed {User.UserNameMaxLength} characters.");

        RuleFor(x => x.UserEmail)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(User.EmailMaxLength).WithMessage($"Email cannot exceed {User.EmailMaxLength} characters.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(User.RawPasswordMinLength).WithMessage($"Password must be at least {User.RawPasswordMinLength} characters long.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.");
    }
}