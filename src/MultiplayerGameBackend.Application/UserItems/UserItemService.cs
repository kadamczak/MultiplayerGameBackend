using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Identity;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Items.Responses;
using MultiplayerGameBackend.Application.UserItems.Responses;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.Application.UserItems;

public class UserItemService(ILogger<UserItemService> logger,
    IMultiplayerGameDbContext dbContext,
    IUserContext userContext) : IUserItemService
{
    public async Task<IEnumerable<ReadUserItemDto>> GetCurrentUserItems(CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException();
        var userId = Guid.Parse(currentUser.Id);

        var userItems = await dbContext.UserItems
            .AsNoTracking()
            .Where(ui => ui.UserId == userId)
            .Select(ui => new ReadUserItemDto
            {
                Id = ui.Id,
                Item = new ReadItemDto
                {
                    Id = ui.Item.Id,
                    Name = ui.Item.Name,
                    Description = ui.Item.Description
                },
                ObtainedAt = ui.ObtainedAt
            })
            .ToListAsync(cancellationToken);

        logger.LogInformation("Fetched {ItemCount} items for user {UserId}", userItems.Count, currentUser.Id);
        return userItems;
    }
}