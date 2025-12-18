using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.UserItemOffers.Requests;
using MultiplayerGameBackend.Application.UserItemOffers.Responses;

namespace MultiplayerGameBackend.Application.UserItemOffers;

public interface IUserItemOfferService
{
    Task<PagedResult<ReadUserItemOfferDto>> GetOffers(GetOffersDto dto, CancellationToken cancellationToken);
    Task CreateOffer(Guid userId, CreateUserItemOfferDto dto, CancellationToken cancellationToken);
    Task DeleteOffer(Guid userId, Guid offerId, CancellationToken cancellationToken);
    Task PurchaseOffer(Guid buyerId, Guid offerId, CancellationToken cancellationToken);
}