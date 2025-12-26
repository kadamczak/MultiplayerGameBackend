using MultiplayerGameBackend.Application.Common.Validators;
using MultiplayerGameBackend.Application.Common;
using FluentValidation;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Identity.Requests.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.UsernameRequired))
            .Length(User.Constraints.UserNameMinLength, User.Constraints.UserNameMaxLength)
            .WithMessage(ValidatorLocalizer.GetString(
                LocalizationKeys.Validation.UsernameLength,
                User.Constraints.UserNameMinLength,
                User.Constraints.UserNameMaxLength));

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.EmailRequired))
            .MaximumLength(User.Constraints.EmailMaxLength).WithMessage(ValidatorLocalizer.GetString(
                LocalizationKeys.Validation.MaxLength,
                "Email",
                User.Constraints.EmailMaxLength))
            .EmailAddress().WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.InvalidEmail));

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.PasswordRequired))
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