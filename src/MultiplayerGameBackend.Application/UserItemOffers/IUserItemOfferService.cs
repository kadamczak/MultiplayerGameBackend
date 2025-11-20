using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.UserItemOffers.Requests;
using MultiplayerGameBackend.Application.UserItemOffers.Responses;

namespace MultiplayerGameBackend.Application.UserItemOffers;

public interface IUserItemOfferService
{
    Task<PagedResult<ReadActiveUserItemOfferDto>> GetActiveOffers(PagedQuery query, CancellationToken cancellationToken);
    Task CreateOffer(CreateUserItemOfferDto dto, CancellationToken cancellationToken);
    Task DeleteOffer(Guid offerId, CancellationToken cancellationToken);
    Task PurchaseOffer(Guid offerId, CancellationToken cancellationToken);
}