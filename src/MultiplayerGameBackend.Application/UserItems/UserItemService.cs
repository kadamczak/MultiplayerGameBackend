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

        logger.LogInformation("Fetched {totalCount} items for user {UserId}", totalCount, currentUser.Id);
        var result = new PagedResult<ReadUserItemDto>(userItems, totalCount, query.PageSize, query.PageNumber);
        return result;
    }
}