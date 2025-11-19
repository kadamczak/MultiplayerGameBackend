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
                        Description = o.UserItem.Item.Description,
                        Type = o.UserItem.Item.Type,
                        ThumbnailUrl = o.UserItem.Item.ThumbnailUrl,
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

    public async Task CreateOffer(CreateUserItemOfferDto dto, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException();
        var userId = Guid.Parse(currentUser.Id);
        logger.LogInformation("User {UserId} attempting to create offer for UserItem {UserItemId}", userId, dto.UserItemId);
        
        var userItem = await dbContext.UserItems
            .Include(ui => ui.Offer)
            .FirstOrDefaultAsync(ui => ui.Id == dto.UserItemId, cancellationToken);
        
        if (userItem is null)
        {
            logger.LogWarning("UserItem {UserItemId} not found", dto.UserItemId);
            throw new NotFoundException(nameof(UserItem), nameof(UserItem.Id), "ID", dto.UserItemId.ToString());
        }
        
        // Check if the user owns this UserItem
        if (userItem.UserId != userId)
        {
            logger.LogWarning("User {UserId} attempted to create offer for UserItem {UserItemId} that doesn't belong to them", userId, dto.UserItemId);
            throw new ForbidException();
        }
        
        // Check if an offer already exists for this UserItem
        if (userItem.Offer is not null)
        {
            logger.LogWarning("Offer already exists for UserItem {UserItemId}", dto.UserItemId);
            throw new ConflictException(nameof(UserItemOffer), nameof(dto.UserItemId), "UserItem", dto.UserItemId.ToString());
        }
        
        var offer = new UserItemOffer
        {
            Id = Guid.NewGuid(),
            UserItemId = dto.UserItemId,
            Price = dto.Price
        };
        
        dbContext.UserItemOffers.Add(offer);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("User {UserId} successfully created offer {OfferId} for UserItem {UserItemId}", userId, offer.Id, dto.UserItemId);
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
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException();
        var buyerId = Guid.Parse(currentUser.Id);
        
        logger.LogInformation("User {BuyerId} attempting to purchase offer {OfferId}", buyerId, offerId);
        
        var offer = await dbContext.UserItemOffers
            .Include(o => o.UserItem)
            .ThenInclude(ui => ui!.User)
            .FirstOrDefaultAsync(o => o.Id == offerId, cancellationToken);
        
        if (offer is null)
        {
            logger.LogWarning("Offer {OfferId} not found", offerId);
            throw new NotFoundException(nameof(UserItemOffer), nameof(UserItemOffer.Id), "ID", offerId.ToString());
        }
        
        // Check if user is trying to buy their own item
        if (offer.UserItem!.UserId == buyerId)
        {
            logger.LogWarning("User {BuyerId} attempted to purchase their own offer {OfferId}", buyerId, offerId);
            throw new UnprocessableEntityException(new Dictionary<string, string[]>
            {
                { "Offer", new[] { "You cannot purchase your own item." } }
            });
        }
        
        var buyer = await dbContext.Users.FindAsync([buyerId], cancellationToken);
        if (buyer is null)
        {
            logger.LogWarning("Buyer {BuyerId} not found", buyerId);
            throw new NotFoundException(nameof(User), nameof(User.Id), "ID", buyerId.ToString());
        }
        
        // Check if buyer has sufficient balance
        if (buyer.Balance < offer.Price)
        {
            logger.LogWarning("User {BuyerId} has insufficient balance ({Balance}) to purchase offer {OfferId} (price: {Price})", 
                buyerId, buyer.Balance, offerId, offer.Price);
            throw new UnprocessableEntityException(new Dictionary<string, string[]>
            {
                { "Balance", new[] { $"Insufficient balance. Required: {offer.Price}, Available: {buyer.Balance}" } }
            });
        }
        
        var seller = offer.UserItem.User!;
        var sellerId = seller.Id;
        
        logger.LogInformation("Processing purchase: Buyer {BuyerId} (Balance: {BuyerBalance}), Seller {SellerId} (Balance: {SellerBalance}), Price: {Price}", 
            buyerId, buyer.Balance, sellerId, seller.Balance, offer.Price);
        
        // Transfer balance
        buyer.Balance -= offer.Price;
        seller.Balance += offer.Price;
        
        // Transfer ownership
        offer.UserItem.UserId = buyerId;
        
        // Remove the offer
        dbContext.UserItemOffers.Remove(offer);
        
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Successfully completed purchase: User {BuyerId} bought UserItem {UserItemId} from User {SellerId} for {Price}", 
            buyerId, offer.UserItem.Id, sellerId, offer.Price);
    }
}