using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Users.Requests;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.Application.Users;

public class UserService(ILogger<UserService> logger,
    UserManager<User> userManager,
    RoleManager<IdentityRole<Guid>> roleManager) : IUserService
{
    public async Task AssignUserRole(Guid id, ModifyUserRoleDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Assigning user role: {@Request}", dto);
        var user = await userManager.FindByIdAsync(id.ToString())
                   ?? throw new NotFoundException(nameof(User), nameof(User.Id), id.ToString());

        var role = await roleManager.FindByNameAsync(dto.RoleName)
                   ?? throw new NotFoundException(nameof(IdentityRole), nameof(IdentityRole.Name), dto.RoleName);

        await userManager.AddToRoleAsync(user, role.Name!);
    }

    public async Task UnassignUserRole(Guid id, ModifyUserRoleDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Unassigning user role: {@Request}", dto);
        var user = await userManager.FindByIdAsync(id.ToString())
                   ?? throw new NotFoundException(nameof(User), nameof(User.Id), id.ToString());

        var role = await roleManager.FindByNameAsync(dto.RoleName)
                   ?? throw new NotFoundException(nameof(IdentityRole), nameof(IdentityRole.Name), dto.RoleName);

        await userManager.RemoveFromRoleAsync(user, role.Name!);
    }
}