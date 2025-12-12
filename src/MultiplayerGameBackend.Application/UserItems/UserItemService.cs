using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Identity;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Items.Responses;
using MultiplayerGameBackend.Application.UserItems.Responses;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.Application.UserItems;

public class UserItemService(ILogger<UserItemService> logger,
    IMultiplayerGameDbContext dbContext) : IUserItemService
{
    public async Task<PagedResult<ReadUserItemDto>> GetCurrentUserItems(Guid userId,  PagedQuery query, CancellationToken cancellationToken)
    {
        var searchPhraseLower = query.SearchPhrase?.ToLower();
        var baseQuery = dbContext.UserItems
            .AsNoTracking()
            .Where(ui => ui.UserId == userId)
            .Include(ui => ui.Item)
            .Where(o => searchPhraseLower == null 
                        || o.Item.Name.ToLower().Contains(searchPhraseLower)
                        || o.Item.Type.ToLower().Contains(searchPhraseLower)
                        || o.Item.Description.ToLower().Contains(searchPhraseLower));
        
        var totalCount = await baseQuery.CountAsync(cancellationToken);
        
        if (query.SortBy is not null)
        {
            var columnsSelector =  new Dictionary<string, Expression<Func<UserItem, object>>>()
            {
                { nameof(Item.Name), r => r.Item.Name },
                { nameof(Item.Type), r => r.Item.Type },
                { nameof(Item.Description), r => r.Item.Description }
            };

            var selectedColumn = columnsSelector[query.SortBy];

            baseQuery = query.SortDirection == SortDirection.Ascending
                ? baseQuery.OrderBy(selectedColumn)
                : baseQuery.OrderByDescending(selectedColumn);
        }
        
        var userItemEntities = baseQuery
            .Skip(query.PageSize * (query.PageNumber - 1))
            .Take(query.PageSize);
        
        var userItems = await userItemEntities
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
        var result = new PagedResult<ReadUserItemDto>(userItems, totalCount, query.PageSize, query.PageNumber);
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
                throw new NotFoundException(nameof(UserItem), nameof(UserItem.Id), "ID", dto.EquippedHeadUserItemId.Value.ToString());
            
            if (headUserItem.UserId != userId)
                throw new ForbidException("Cannot equip items that don't belong to you.");
            
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
                throw new NotFoundException(nameof(UserItem), nameof(UserItem.Id), "ID", dto.EquippedBodyUserItemId.Value.ToString());
            
            if (bodyUserItem.UserId != userId)
                throw new ForbidException("Cannot equip items that don't belong to you.");
            
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
            throw new NotFoundException(nameof(UserCustomization), nameof(UserCustomization.UserId), "User ID", userId.ToString());

        // Update the equipped items
        customization.EquippedHeadUserItemId = dto.EquippedHeadUserItemId;
        customization.EquippedBodyUserItemId = dto.EquippedBodyUserItemId;

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated equipped items for user {UserId}: Head={HeadItemId}, Body={BodyItemId}", 
            userId, dto.EquippedHeadUserItemId, dto.EquippedBodyUserItemId);
    }
}