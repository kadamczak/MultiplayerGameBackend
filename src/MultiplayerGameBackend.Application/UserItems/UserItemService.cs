using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Items.Responses;
using MultiplayerGameBackend.Application.UserItems.Requests;
using MultiplayerGameBackend.Application.UserItems.Responses;
using MultiplayerGameBackend.Application.UserItems.Specifications;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.Application.UserItems;

public class UserItemService(ILogger<UserItemService> logger,
    IMultiplayerGameDbContext dbContext,
    ILocalizationService localizationService) : IUserItemService
{
    public async Task<PagedResult<ReadUserItemDto>> GetUserItems(Guid userId,  GetUserItemsDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching sent friend requests for user {CurrentUserId}", userId);
        var searchPhraseLower = dto.PagedQuery.SearchPhrase?.ToLower();
        
        // Build base query for counting (without includes for performance)
        var countQuery = dbContext.UserItems
            .AsNoTracking()
            .Where(ui => ui.UserId == userId)
            .ApplySearchFilter(
                searchPhraseLower,
                UserItemSpecifications.SearchByNameTypeOrDescription(searchPhraseLower));

        var totalCount = await countQuery.CountAsync(cancellationToken);
        
        var dataQuery = dbContext.UserItems
            .AsNoTracking()
            .Where(ui => ui.UserId == userId)
            .Include(ui => ui.Item)
            .ApplySearchFilter(
                searchPhraseLower,
                UserItemSpecifications.SearchByNameTypeOrDescription(searchPhraseLower))
            .ApplySorting(
                dto.PagedQuery.SortBy,
                dto.PagedQuery.SortDirection,
                UserItemSortingSelectors.ByNameTypeOrDescription(),
                defaultSort: fr => fr.Item.Name)
            .ApplyPaging(dto.PagedQuery);
        
        var userItems = await dataQuery
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
            .Select(x => new ReadUserItemDto
            {
                Id = x.Id,
                Item = new ReadItemDto
                {
                    Id = x.Item.Id,
                    Name = x.Item.Name,
                    Description = x.Item.Description,
                    Type = x.Item.Type,
                    ThumbnailUrl = x.Item.ThumbnailUrl,
                },
                ActiveOfferId = x.ActiveOffer != null ? x.ActiveOffer.Id : null,
                ActiveOfferPrice = x.ActiveOffer != null ? x.ActiveOffer.Price : null,
            })
            .ToListAsync(cancellationToken);
        
        logger.LogInformation("Fetched {totalCount} items for user {UserId}", totalCount, userId);
        var result = new PagedResult<ReadUserItemDto>(userItems, totalCount, dto.PagedQuery.PageSize, dto.PagedQuery.PageNumber);
        return result;
    }

    public async Task UpdateEquippedUserItems(Guid userId, UpdateEquippedUserItemsDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating equipped items for user {UserId}", userId);

        // Validate head item if provided
        if (dto.EquippedHeadUserItemId.HasValue)
        {
            var headUserItem = await dbContext.UserItems
                .Include(ui => ui.Item)
                .FirstOrDefaultAsync(ui => ui.Id == dto.EquippedHeadUserItemId.Value, cancellationToken);
            
            if (headUserItem is null)
                throw new NotFoundException(localizationService.GetString(LocalizationKeys.Errors.UserItemNotFound));
            
            if (headUserItem.UserId != userId)
                throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.CannotEquipItemsNotOwned));
            
            if (headUserItem.Item?.Type != ItemTypes.EquippableOnHead)
                throw new UnprocessableEntityException(new Dictionary<string, string[]>
                {
                    { nameof(dto.EquippedHeadUserItemId), new[] { "The specified item is not a Head item." } }
                });
        }

        // Validate body item if provided
        if (dto.EquippedBodyUserItemId.HasValue)
        {
            var bodyUserItem = await dbContext.UserItems
                .Include(ui => ui.Item)
                .FirstOrDefaultAsync(ui => ui.Id == dto.EquippedBodyUserItemId.Value, cancellationToken);
            
            if (bodyUserItem is null)
                throw new NotFoundException(localizationService.GetString(LocalizationKeys.Errors.UserItemNotFound));
            
            if (bodyUserItem.UserId != userId)
                throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.CannotEquipItemsNotOwned));
            
            if (bodyUserItem.Item?.Type != ItemTypes.EquippableOnBody)
                throw new UnprocessableEntityException(new Dictionary<string, string[]>
                {
                    { nameof(dto.EquippedBodyUserItemId), new[] { "The specified item is not a Body item." } }
                });
        }

        // Get the user's customization
        var customization = await dbContext.UserCustomizations
            .FirstOrDefaultAsync(uc => uc.UserId == userId, cancellationToken);

        if (customization is null)
            throw new NotFoundException(localizationService.GetString(LocalizationKeys.Errors.UserCustomizationNotFound));

        // Update the equipped items
        customization.EquippedHeadUserItemId = dto.EquippedHeadUserItemId;
        customization.EquippedBodyUserItemId = dto.EquippedBodyUserItemId;

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated equipped items for user {UserId}: Head={HeadItemId}, Body={BodyItemId}", 
            userId, dto.EquippedHeadUserItemId, dto.EquippedBodyUserItemId);
    }
}