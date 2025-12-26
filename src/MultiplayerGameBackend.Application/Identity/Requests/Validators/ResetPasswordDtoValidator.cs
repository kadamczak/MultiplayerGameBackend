using MultiplayerGameBackend.Application.Common.Validators;
using MultiplayerGameBackend.Application.Common;
using FluentValidation;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Identity.Requests.Validators;

public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.EmailRequired))
            .MaximumLength(User.Constraints.EmailMaxLength).WithMessage(ValidatorLocalizer.GetString(
                LocalizationKeys.Validation.MaxLength,
                "Email",
                User.Constraints.EmailMaxLength))
            .EmailAddress().WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.InvalidEmail));
        
        RuleFor(x => x.ResetToken)
            .NotEmpty().WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.Required, "Reset token"));
        
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.NewPasswordRequired))
            .Length(User.Constraints.RawPasswordMinLength, User.Constraints.RawPasswordMaxLength)
            .WithMessage(ValidatorLocalizer.GetString(
                LocalizationKeys.Validation.PasswordLength,
                User.Constraints.RawPasswordMinLength,
                User.Constraints.RawPasswordMaxLength))
            .Matches(@"[A-Z]").WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.PasswordMustContainUppercase))
            .Matches(@"[a-z]").WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.PasswordMustContainLowercase))
            .Matches(@"\d").WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.PasswordMustContainDigit));
    }
}