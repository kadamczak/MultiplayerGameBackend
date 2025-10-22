using FluentValidation;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Users.Requests.Validators;

public class ModifyUserRoleDtoValidator : AbstractValidator<ModifyUserRoleDto>
{
    public ModifyUserRoleDtoValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role is required.")
            .Must(UserRoles.IsValidRole)
            .WithMessage($"Role must be one of the following: {string.Join(", ", UserRoles.AllRoles)}");
    }
}