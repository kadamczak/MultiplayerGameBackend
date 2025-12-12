using MultiplayerGameBackend.Application.MerchantItemOffers.Responses;

namespace MultiplayerGameBackend.Application.MerchantItemOffers;

public interface IMerchantItemOfferService
{
    Task<IEnumerable<ReadMerchantOfferDto>> GetOffers(int merchantId, CancellationToken cancellationToken);
    Task PurchaseOffer(Guid userId, int offerId, CancellationToken cancellationToken);
}