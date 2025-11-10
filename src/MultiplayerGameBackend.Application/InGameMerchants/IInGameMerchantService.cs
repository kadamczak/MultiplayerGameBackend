using MultiplayerGameBackend.Application.InGameMerchants.Responses;

namespace MultiplayerGameBackend.Application.InGameMerchants;

public interface IInGameMerchantService
{
    Task<IEnumerable<ReadMerchantOfferDto>> GetOffers(int merchantId, CancellationToken cancellationToken);
    Task PurchaseOffer(int offerId, CancellationToken cancellationToken);
}