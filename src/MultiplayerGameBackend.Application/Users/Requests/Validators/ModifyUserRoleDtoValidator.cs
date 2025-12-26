using MultiplayerGameBackend.Application.Common.Validators;
using MultiplayerGameBackend.Application.Common;
using FluentValidation;
using MultiplayerGameBackend.Domain.Constants;

namespace MultiplayerGameBackend.Application.Users.Requests.Validators;

public class ModifyUserRoleDtoValidator : AbstractValidator<ModifyUserRoleDto>
{
    public ModifyUserRoleDtoValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.RoleNameRequired))
            .Must(UserRoles.IsValidRole)
            .WithMessage(ValidatorLocalizer.GetString(
                LocalizationKeys.Validation.InvalidValue,
                string.Join(", ", UserRoles.AllRoles)));
    }
}