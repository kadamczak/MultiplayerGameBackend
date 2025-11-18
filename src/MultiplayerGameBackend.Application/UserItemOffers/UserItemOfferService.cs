using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Identity;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.UserItemOffers.Responses;

namespace MultiplayerGameBackend.Application.UserItemOffers;

public class UserItemOfferService(ILogger<UserItemOfferService> logger,
    IMultiplayerGameDbContext dbContext,
    IUserContext userContext) : IUserItemOfferService
{
    public Task<IEnumerable<ReadUserItemOfferDto>> GetAllOffers(CancellationToken cancellationToken)
    {
        
    }

    public async Task PurchaseOffer(int offerId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}