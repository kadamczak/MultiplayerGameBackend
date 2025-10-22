using FluentValidation;
using MultiplayerGameBackend.Application.Users.Requests;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Identity.Requests.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required.")
            .Length(User.Constraints.UserNameMinLength, User.Constraints.UserNameMaxLength)
            .WithMessage($"Username must be between {User.Constraints.UserNameMinLength} and {User.Constraints.UserNameMaxLength} characters long.");

        RuleFor(x => x.UserEmail)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(User.Constraints.EmailMaxLength).WithMessage($"Email cannot exceed {User.Constraints.EmailMaxLength} characters.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(User.Constraints.RawPasswordMinLength).WithMessage($"Password must be at least {User.Constraints.RawPasswordMinLength} characters long.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.");
    }
}