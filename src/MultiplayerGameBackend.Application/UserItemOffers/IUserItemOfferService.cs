using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.UserItemOffers.Requests;
using MultiplayerGameBackend.Application.UserItemOffers.Responses;

namespace MultiplayerGameBackend.Application.UserItemOffers;

public interface IUserItemOfferService
{
    Task<PagedResult<ReadUserItemOfferDto>> GetOffers(PagedQuery query, bool showActive, CancellationToken cancellationToken);
    Task CreateOffer(CreateUserItemOfferDto dto, CancellationToken cancellationToken);
    Task DeleteOffer(Guid offerId, CancellationToken cancellationToken);
    Task PurchaseOffer(Guid offerId, CancellationToken cancellationToken);
}