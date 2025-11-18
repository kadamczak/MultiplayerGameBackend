using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Identity;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.UserItemOffers.Responses;
using MultiplayerGameBackend.Application.UserItems.Responses;
using MultiplayerGameBackend.Application.Items.Responses;
using MultiplayerGameBackend.Application.UserItemOffers.Requests;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.Application.UserItemOffers;

public class UserItemOfferService(ILogger<UserItemOfferService> logger,
    IMultiplayerGameDbContext dbContext,
    IUserContext userContext) : IUserItemOfferService
{
    public async Task<IEnumerable<ReadUserItemOfferDto>> GetAllOffers(CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching all user item offers");
        
        var offers = await dbContext.UserItemOffers
            .AsNoTracking()
            .Include(o => o.UserItem)
            .ThenInclude(ui => ui!.Item)
            .Include(o => o.UserItem!.User)
            .Select(o => new ReadUserItemOfferDto
            {
                Id = o.Id,
                Price = o.Price,
                UserItem = new ReadUserItemDto
                {
                    Id = o.UserItem!.Id,
                    Item = new ReadItemDto
                    {
                        Id = o.UserItem.Item!.Id,
                        Name = o.UserItem.Item.Name,
                        Description = o.UserItem.Item.Description
                    },
                    UserId = o.UserItem.UserId,
                    UserName = o.UserItem.User!.UserName,
                    OfferId = o.Id
                }
            })
            .ToListAsync(cancellationToken);
        
        logger.LogInformation("Fetched {OfferCount} user item offers", offers.Count);
        return offers;
    }

    public Task CreateOffer(CreateUserItemOfferDto dto, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteOffer(Guid offerId, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException();
        var userId = Guid.Parse(currentUser.Id);
        logger.LogInformation("User {UserId} attempting to delete offer {OfferId}", userId, offerId);
        
        var offer = await dbContext.UserItemOffers
            .Include(o => o.UserItem)
            .FirstOrDefaultAsync(o => o.Id == offerId, cancellationToken);
        
        if (offer is null)
        {
            logger.LogWarning("Offer {OfferId} not found", offerId);
            throw new NotFoundException(nameof(UserItemOffer), nameof(UserItemOffer.Id), "ID", offerId.ToString());
        }
        
        if (offer.UserItem?.UserId != userId)
        {
            logger.LogWarning("User {UserId} attempted to delete offer {OfferId} that doesn't belong to them", userId, offerId);
            throw new ForbidException();
        }
        
        dbContext.UserItemOffers.Remove(offer);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("User {UserId} successfully deleted offer {OfferId}", userId, offerId);
    }
    
    public async Task PurchaseOffer(Guid offerId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}