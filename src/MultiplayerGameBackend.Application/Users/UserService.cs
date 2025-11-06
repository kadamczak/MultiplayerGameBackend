using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Identity;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Users.Requests;
using MultiplayerGameBackend.Application.Users.Responses;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.Application.Users;

public class UserService(ILogger<UserService> logger,
    UserManager<User> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IUserContext userContext) : IUserService
{
    public async Task AssignUserRole(Guid id, ModifyUserRoleDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Assigning user role: {@Request}", dto);
        var user = await userManager.FindByIdAsync(id.ToString())
                   ?? throw new NotFoundException(nameof(User), nameof(User.Id), "Id", id.ToString());

        var role = await roleManager.FindByNameAsync(dto.RoleName)
                   ?? throw new NotFoundException(nameof(IdentityRole), nameof(IdentityRole.Name), "Name", dto.RoleName);

        await userManager.AddToRoleAsync(user, role.Name!);
    }

    public async Task UnassignUserRole(Guid id, ModifyUserRoleDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Unassigning user role: {@Request}", dto);
        var user = await userManager.FindByIdAsync(id.ToString())
                   ?? throw new NotFoundException(nameof(User), nameof(User.Id), "Id", id.ToString());

        var role = await roleManager.FindByNameAsync(dto.RoleName)
                   ?? throw new NotFoundException(nameof(IdentityRole), nameof(IdentityRole.Name), "Name", dto.RoleName);

        await userManager.RemoveFromRoleAsync(user, role.Name!);
    }

    public async Task<UserGameInfoDto> GetCurrentUserGameInfo(CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser();
        
        if (currentUser is null)
        {
            logger.LogWarning("Attempt to fetch data for unauthenticated user");
            throw new ForbidException();
        }
        
        logger.LogInformation("Fetching game info for user {UserId}", currentUser.Id);
        
        var userId = Guid.Parse(currentUser.Id);
        var user = await userManager.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException(nameof(User), nameof(User.Id), "Id", userId.ToString());
        
        var userGameInfo = new UserGameInfoDto
        {
            Id = userId,
            UserName = currentUser.UserName!,
            Balance = user.Balance
        };
        
        return userGameInfo;
    }
}