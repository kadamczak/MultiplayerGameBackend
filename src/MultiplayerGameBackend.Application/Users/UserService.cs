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
    public async Task RegisterUser(RegisterDto dto)
    {
        var userSameName = await userManager.FindByNameAsync(dto.UserName);
        if (userSameName is not null)
            throw new ConflictException(nameof(User), dto.UserName);

        var userSameEmail = await userManager.FindByEmailAsync(dto.UserEmail);
        if (userSameEmail is not null)
            throw new ConflictException(nameof(User), dto.UserEmail);

        var user = new User { UserName = dto.UserName, Email = dto.UserEmail };

        var createResult = await userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            throw new ApplicationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));

        var roleResult = await userManager.AddToRoleAsync(user, UserRoles.User);
        if (!roleResult.Succeeded)
            throw new ApplicationException(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
    }
    
    public async Task AssignUserRole(ModifyUserRoleDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Assigning user role: {@Request}", dto);
        var user = await userManager.FindByEmailAsync(dto.UserEmail)
                   ?? throw new NotFoundException(nameof(User), dto.UserEmail);

        var role = await roleManager.FindByNameAsync(dto.RoleName)
                   ?? throw new NotFoundException(nameof(IdentityRole), dto.RoleName);

        await userManager.AddToRoleAsync(user, role.Name!);
    }

    public async Task UnassignUserRole(ModifyUserRoleDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Unassigning user role: {@Request}", dto);
        var user = await userManager.FindByEmailAsync(dto.UserEmail)
                   ?? throw new NotFoundException(nameof(User), dto.UserEmail);

        var role = await roleManager.FindByNameAsync(dto.RoleName)
                   ?? throw new NotFoundException(nameof(IdentityRole), dto.RoleName);

        await userManager.RemoveFromRoleAsync(user, role.Name!);
    }
}