using MultiplayerGameBackend.Application.UserItemOffers.Responses;
using MultiplayerGameBackend.Application.UserItems.Responses;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Common.Mappings;

public class UserItemOfferMapper
{
    private readonly ItemMapper _itemMapper;

    public UserItemOfferMapper(ItemMapper itemMapper)
    {
        _itemMapper = itemMapper;
    }

    public ReadUserItemOfferDto MapToReadUserItemOfferDto(UserItemOffer offer)
    {
        return new ReadUserItemOfferDto
        {
            Id = offer.Id,
            Price = offer.Price,
            SellerId = offer.SellerId,
            SellerUsername = offer.Seller?.UserName ?? string.Empty,
            PublishedAt = offer.PublishedAt,
            BuyerId = offer.BuyerId,
            BuyerUsername = offer.Buyer?.UserName,
            BoughtAt = offer.BoughtAt,
            UserItem = new ReadUserItemDto
            {
                Id = offer.UserItem!.Id,
                Item = _itemMapper.Map(offer.UserItem.Item)!,
                ActiveOfferId = null,
                ActiveOfferPrice = null
            }
        };
    }
}

