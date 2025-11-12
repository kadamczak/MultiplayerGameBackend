using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Identity;
using MultiplayerGameBackend.Application.InGameMerchants.Responses;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Items.Responses;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.Application.InGameMerchants;

public class InGameMerchantService(ILogger<InGameMerchantService> logger,
    IMultiplayerGameDbContext dbContext,
    IUserContext userContext) : IInGameMerchantService
{
    public async Task<IEnumerable<ReadMerchantOfferDto>> GetOffers(int merchantId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching offers for merchant {MerchantId}", merchantId);
        
        // Check if merchant exists
        var merchantExists = await dbContext.InGameMerchants
            .AnyAsync(m => m.Id == merchantId, cancellationToken);
        
        if (!merchantExists)
        {
            logger.LogWarning("Merchant {MerchantId} not found", merchantId);
            throw new NotFoundException(nameof(InGameMerchant), nameof(InGameMerchant.Id), "ID", merchantId.ToString());
        }
        
        // Retrieve all offers for the specified merchant from the database
        var offers = await dbContext.MerchantItemOffers
            .AsNoTracking()
            .Where(o => o.MerchantId == merchantId)
            .Select(o => new ReadMerchantOfferDto
            {
                Id = o.Id,
                Item = new ReadItemDto
                {
                    Id = o.Item!.Id,
                    Name = o.Item.Name,
                    Description = o.Item.Description
                },
                Price = o.Price
            })
            .ToListAsync(cancellationToken);
        
        logger.LogInformation("Fetched {OfferCount} offers for merchant {MerchantId}", offers.Count, merchantId);
        return offers;
    }

    public async Task PurchaseOffer(int offerId, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser();

        if (currentUser is null)
        {
            logger.LogWarning("Attempt to purchase offer by unauthenticated user");
            throw new ForbidException();
        }

        var userId = Guid.Parse(currentUser.Id);
        logger.LogInformation("User {UserId} attempting to purchase offer {OfferId}", userId, offerId);

        // Fetch the offer with related item
        var offer = await dbContext.MerchantItemOffers
            .Include(o => o.Item)
            .FirstOrDefaultAsync(o => o.Id == offerId, cancellationToken);

        if (offer is null)
        {
            logger.LogWarning("Offer {OfferId} not found", offerId);
            throw new NotFoundException(nameof(MerchantItemOffer),
                nameof(MerchantItemOffer.Id),
                "ID",
                offerId.ToString());
        }

        // Fetch the user with balance
        var user = await dbContext.Users.FindAsync([userId], cancellationToken);

        if (user is null)
        {
            logger.LogWarning("User {UserId} not found", userId);
            throw new NotFoundException(nameof(User), nameof(User.Id), "ID", userId.ToString());
        }

        // Check if user has sufficient balance
        if (user.Balance < offer.Price)
        {
            logger.LogWarning("User {UserId} has insufficient balance. Required: {Price}, Available: {Balance}", 
                userId, offer.Price, user.Balance);
            throw new UnprocessableEntityException(new Dictionary<string, string[]>
            {
                { "Balance", [$"Insufficient balance. Required: {offer.Price}, Available: {user.Balance}"] }
            });
        }

        // Deduct balance
        user.Balance -= offer.Price;

        // Add item to user's inventory
        var userItem = new UserItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ItemId = offer.ItemId,
        };

        await dbContext.UserItems.AddAsync(userItem, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("User {UserId} successfully purchased offer {OfferId}", userId, offerId);
    }
}