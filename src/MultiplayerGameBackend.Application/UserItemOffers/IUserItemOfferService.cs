using MultiplayerGameBackend.Application.UserItemOffers.Requests;
using MultiplayerGameBackend.Application.UserItemOffers.Responses;

namespace MultiplayerGameBackend.Application.UserItemOffers;

public interface IUserItemOfferService
{
    Task<IEnumerable<ReadUserItemOfferDto>> GetAllOffers(CancellationToken cancellationToken);
    Task CreateOffer(CreateUserItemOfferDto dto, CancellationToken cancellationToken);
    Task DeleteOffer(Guid offerId, CancellationToken cancellationToken);
    Task PurchaseOffer(Guid offerId, CancellationToken cancellationToken);
}