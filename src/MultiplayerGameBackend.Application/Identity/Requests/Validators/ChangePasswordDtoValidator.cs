using FluentValidation;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Common.Validators;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Identity.Requests.Validators;

public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.CurrentPasswordRequired));

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

