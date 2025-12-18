using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Identity;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.UserItemOffers.Responses;
using MultiplayerGameBackend.Application.UserItems.Responses;
using MultiplayerGameBackend.Application.Items.Responses;
using MultiplayerGameBackend.Application.UserItemOffers.Requests;
using MultiplayerGameBackend.Application.UserItemOffers.Specifications;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.Application.UserItemOffers;

public class UserItemOfferService(ILogger<UserItemOfferService> logger,
    IMultiplayerGameDbContext dbContext) : IUserItemOfferService
{
    public async Task<PagedResult<ReadUserItemOfferDto>> GetOffers(PagedQuery query, bool showActive, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching user item offers");
        var searchPhraseLower = query.SearchPhrase?.ToLower();
        
        var baseQuery = dbContext.UserItemOffers
            .AsNoTracking();
        
        baseQuery = showActive 
            ? baseQuery.Where(o => o.BuyerId == null)
            : baseQuery.Where(o => o.BuyerId != null);
        
        baseQuery = baseQuery
            .Include(o => o.UserItem)
            .ThenInclude(ui => ui!.Item)
            .Include(o => o.Seller)
            .Include(o => o.Buyer);
            
        if (showActive)
        {
            baseQuery = baseQuery.Where(o => searchPhraseLower == null 
                || o.UserItem.Item.Name.ToLower().Contains(searchPhraseLower)
                || o.UserItem.Item.Type.ToLower().Contains(searchPhraseLower)
                || o.Seller.UserName.ToLower().Contains(searchPhraseLower));
        }
        else
        {
            baseQuery = baseQuery.Where(o => searchPhraseLower == null 
                || o.UserItem.Item.Name.ToLower().Contains(searchPhraseLower)
                || o.UserItem.Item.Type.ToLower().Contains(searchPhraseLower)
                || o.Seller.UserName.ToLower().Contains(searchPhraseLower)
                || o.Buyer.UserName.ToLower().Contains(searchPhraseLower));
        }
            
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        if (query.SortBy is not null)
        {
            var columnsSelector =  showActive
                ? UserItemOfferSortingSelectors.ForActiveOffers
                : UserItemOfferSortingSelectors.ForInactiveOffers;

            var selectedColumn = columnsSelector[query.SortBy];

            baseQuery = query.SortDirection == SortDirection.Ascending
                ? baseQuery.OrderBy(selectedColumn)
                : baseQuery.OrderByDescending(selectedColumn);
        }
        
        var offerEntities = baseQuery
            .Skip(query.PageSize * (query.PageNumber - 1))
            .Take(query.PageSize);
        
        var offers = await offerEntities
            .Select(o => new ReadUserItemOfferDto
            {
                Id = o.Id,
                Price = o.Price,
                
                SellerId = o.SellerId,
                SellerUsername = o.Seller!.UserName,
                PublishedAt = o.PublishedAt,
                
                BuyerId = o.BuyerId,
                BuyerUsername = o.Buyer!.UserName,
                BoughtAt = o.BoughtAt,
                
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
                    }
                }
            })
            .ToListAsync(cancellationToken);
        
        var result = new PagedResult<ReadUserItemOfferDto>(offers, totalCount, query.PageSize, query.PageNumber);
        return result;
    }
    
    public async Task CreateOffer(Guid userId, CreateUserItemOfferDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {UserId} attempting to create offer for UserItem {UserItemId}", userId, dto.UserItemId);
        
        var userItem = await dbContext.UserItems
            .Include(ui => ui.Offers)
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
            throw new ForbidException("You do not own this Item.");
        }
        
        // Check if an active offer already exists for this UserItem
        if (userItem.Offers.Any(o => o.BuyerId is null))
        {
            logger.LogWarning("Active offer already exists for UserItem {UserItemId}", dto.UserItemId);
            throw new ConflictException(nameof(UserItemOffer), nameof(dto.UserItemId), "UserItem", dto.UserItemId.ToString());
        }
        
        var offer = new UserItemOffer
        {
            Id = Guid.NewGuid(),
            UserItemId = dto.UserItemId,
            Price = dto.Price,
            SellerId = userId,
            PublishedAt = DateTime.UtcNow,
            BuyerId = null
        };
        
        dbContext.UserItemOffers.Add(offer);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("User {UserId} successfully created offer {OfferId} for UserItem {UserItemId}", userId, offer.Id, dto.UserItemId);
    }

    public async Task DeleteOffer(Guid userId, Guid offerId, CancellationToken cancellationToken)
    {
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
            throw new ForbidException("You do not own this offer.");
        }
        
        dbContext.UserItemOffers.Remove(offer);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("User {UserId} successfully deleted offer {OfferId}", userId, offerId);
    }
    
    public async Task PurchaseOffer(Guid buyerId, Guid offerId, CancellationToken cancellationToken)
    {
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
        
        // Transfer ownership and check as bought
        offer.UserItem.UserId = buyerId;
        offer.BuyerId = buyerId;
        offer.BoughtAt = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Successfully completed purchase: User {BuyerId} bought UserItem {UserItemId} from User {SellerId} for {Price}", 
            buyerId, offer.UserItem.Id, sellerId, offer.Price);
    }
}