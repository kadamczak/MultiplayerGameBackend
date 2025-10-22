using FluentValidation;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Users.Requests.Validators;

public class ModifyUserRoleDtoValidator : AbstractValidator<ModifyUserRoleDto>
{
    public ModifyUserRoleDtoValidator()
    {
        var availableRoles = UserRoles.AllRoles;
        
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => availableRoles.Contains(role)).WithMessage($"Role must be one of the following: {string.Join(", ", availableRoles)}");
    }
}