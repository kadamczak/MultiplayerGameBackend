using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Common.Mappings;
using MultiplayerGameBackend.Application.Identity;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Users.Requests;
using MultiplayerGameBackend.Application.Users.Responses;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.Application.Users;

public class UserService(ILogger<UserService> logger,
    UserManager<User> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IUserContext userContext,
    IMultiplayerGameDbContext dbContext,
    UserCustomizationMapper customizationMapper) : IUserService
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
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException();
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
    
    public async Task<ReadUserCustomizationDto?> GetCurrentUserCustomization(CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException();
        var userId = Guid.Parse(currentUser.Id);
        
        var customization = await dbContext.UserCustomizations
            .FirstOrDefaultAsync(uc => uc.UserId == userId, cancellationToken);
        
        if (customization is null)
        {
            logger.LogInformation("No customization found for user {UserId}", userId);
            return null;
        }

        var customizationDto = customizationMapper.Map(customization);
        logger.LogInformation("Fetched customization for user {UserId}", userId);
        return customizationDto;
    }

    public async Task UpdateUserCustomization(UpdateUserCustomizationDto dto, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException();
        var userId = Guid.Parse(currentUser.Id);
        
        // Verify user exists
        _ = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException(nameof(User), nameof(User.Id), "Id", userId.ToString());

        // Check if user already has customization
        var existingCustomization = await dbContext.UserCustomizations
            .FirstOrDefaultAsync(uc => uc.UserId == userId, cancellationToken);

        if (existingCustomization is not null)
        {
            customizationMapper.UpdateFromDto(dto, existingCustomization);
            existingCustomization.UserId = userId;
            
            logger.LogInformation("Updated existing customization for user {UserId}", userId);
        }
        else
        {
            var newCustomization = customizationMapper.Map(dto);
            newCustomization.UserId = userId;

            await dbContext.UserCustomizations.AddAsync(newCustomization, cancellationToken);
            logger.LogInformation("Created new customization for user {UserId}", userId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Successfully saved customization for user {UserId}", userId);
    }

}