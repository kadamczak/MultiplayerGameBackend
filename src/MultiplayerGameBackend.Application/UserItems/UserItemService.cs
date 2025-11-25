using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    IMultiplayerGameDbContext dbContext,
    IUserContext userContext) : IUserItemService
{
    public async Task<PagedResult<ReadUserItemDto>> GetCurrentUserItems(PagedQuery query, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException("User must be authenticated to access their items.");
        var userId = Guid.Parse(currentUser.Id);
        
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
                HasActiveOffer = dbContext.UserItemOffers.Any(o => o.UserItemId == ui.Id && o.BuyerId == null),
            })
            .ToListAsync(cancellationToken);

        logger.LogInformation("Fetched {totalCount} items for user {UserId}", totalCount, currentUser.Id);
        var result = new PagedResult<ReadUserItemDto>(userItems, totalCount, query.PageSize, query.PageNumber);
        return result;
    }
}