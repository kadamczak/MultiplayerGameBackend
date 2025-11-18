using MultiplayerGameBackend.Application.UserItemOffers.Responses;

namespace MultiplayerGameBackend.Application.UserItemOffers;

public interface IUserItemOfferService
{
    Task<IEnumerable<ReadUserItemOfferDto>> GetAllOffers(CancellationToken cancellationToken);
    Task PurchaseOffer(int offerId, CancellationToken cancellationToken);
}