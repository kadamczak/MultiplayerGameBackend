using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Common.Mappings;
using MultiplayerGameBackend.Application.Identity;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Items.Responses;
using MultiplayerGameBackend.Application.UserItems.Responses;
using MultiplayerGameBackend.Application.Users.Requests;
using MultiplayerGameBackend.Application.Users.Responses;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.Application.Users;

public class UserService(ILogger<UserService> logger,
    UserManager<User> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IMultiplayerGameDbContext dbContext,
    UserCustomizationMapper customizationMapper,
    IImageService imageService) : IUserService
{

    public async Task AssignUserRole(Guid userId, ModifyUserRoleDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Assigning user role: {@Request}", dto);
        var user = await userManager.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException(nameof(User), nameof(User.Id), "Id", userId.ToString());

        var role = await roleManager.FindByNameAsync(dto.RoleName)
                   ?? throw new NotFoundException(nameof(IdentityRole), nameof(IdentityRole.Name), "Name", dto.RoleName);

        await userManager.AddToRoleAsync(user, role.Name!);
    }

    public async Task UnassignUserRole(Guid userId, ModifyUserRoleDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Unassigning user role: {@Request}", dto);
        var user = await userManager.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException(nameof(User), nameof(User.Id), "Id", userId.ToString());

        var role = await roleManager.FindByNameAsync(dto.RoleName)
                   ?? throw new NotFoundException(nameof(IdentityRole), nameof(IdentityRole.Name), "Name", dto.RoleName);

        await userManager.RemoveFromRoleAsync(user, role.Name!);
    }

    public async Task<UserGameInfoDto> GetCurrentUserGameInfo(Guid userId, bool includeCustomization, bool includeUserItems, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException(nameof(User), nameof(User.Id), "Id", userId.ToString());
        
        ReadUserCustomizationDto? customizationDto = null;
        
        if (includeCustomization)
        {
            var customization = await dbContext.UserCustomizations
                .FirstOrDefaultAsync(uc => uc.UserId == userId, cancellationToken);
            
            if (customization is not null)
                customizationDto = customizationMapper.Map(customization);
        }

        List<ReadUserItemDto>? userItems = null;
        if (includeUserItems)
        {
            var userItemEntities = await dbContext.UserItems
                .AsNoTracking()
                .Where(i => i.UserId == userId)
                .Include(i => i.Item)
                .ToListAsync(cancellationToken);
            
            userItems = userItemEntities
                .Select(ui => new
                {
                    ui.Id,
                    ui.Item,
                    ActiveOffer = dbContext.UserItemOffers
                        .AsNoTracking()
                        .Where(o => o.UserItemId == ui.Id && o.BuyerId == null)
                        .Select(o => new { o.Id, o.Price })
                        .FirstOrDefault()
                })
                .Select(ui => new ReadUserItemDto
                {
                    Id = ui.Id,
                    Item = new ReadItemDto
                    {
                        Id = ui.Item.Id,
                        Name = ui.Item.Name,
                        Description = ui.Item.Description,
                        Type = ui.Item.Type,
                        ThumbnailUrl = ui.Item.ThumbnailUrl,
                    },
                    ActiveOfferId = ui.ActiveOffer?.Id,
                    ActiveOfferPrice = ui.ActiveOffer?.Price,
                })
                .ToList();
        }
        
        var userGameInfo = new UserGameInfoDto
        {
            Id = userId,
            UserName = user.UserName!,
            Balance = user.Balance,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Customization = customizationDto,
            UserItems = userItems
        };
        
        return userGameInfo;
    }
    

    public async Task UpdateUserAppearance(Guid userId, UpdateUserAppearanceDto dto, CancellationToken cancellationToken)
    {
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

    public async Task<string> UploadProfilePicture(Guid userId, Stream imageStream, string fileName, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException(nameof(User), nameof(User.Id), "Id", userId.ToString());

        // Delete old profile picture if exists
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            await imageService.DeleteProfilePictureAsync(user.ProfilePictureUrl, cancellationToken);

        // Save new profile picture
        var profilePictureUrl = await imageService.SaveProfilePictureAsync(imageStream, fileName, cancellationToken);
        
        // Update user's data
        user.ProfilePictureUrl = profilePictureUrl;
        await userManager.UpdateAsync(user);
        logger.LogInformation("Profile picture uploaded for user {UserId}", userId);
        
        return profilePictureUrl;
    }

    public async Task DeleteProfilePicture(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException(nameof(User), nameof(User.Id), "Id", userId.ToString());

        if (string.IsNullOrEmpty(user.ProfilePictureUrl))
            throw new NotFoundException(nameof(User.ProfilePictureUrl), "ProfilePictureUrl", "Profile Picture URL", "not set");
        
        await imageService.DeleteProfilePictureAsync(user.ProfilePictureUrl, cancellationToken);
        
        // Update user's data
        user.ProfilePictureUrl = null;
        await userManager.UpdateAsync(user);
        logger.LogInformation("Profile picture deleted for user {UserId}", userId);
    }
}