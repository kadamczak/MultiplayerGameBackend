using System.Linq.Expressions;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.UserItemOffers.Specifications;

public static class UserItemOfferSortingSelectors
{
    public static Dictionary<string, Expression<Func<UserItemOffer, object>>> ForActiveOffers()
        => new()
        {
            { nameof(UserItem.Item.Name), o => o.UserItem.Item.Name },
            { nameof(UserItem.Item.Type), o => o.UserItem.Item.Type },
            { "SellerUserName", o => o.Seller.UserName! },
            { nameof(UserItemOffer.Price), o => o.Price },
            { nameof(UserItemOffer.PublishedAt), o => o.PublishedAt },
        };
    
    public static Dictionary<string, Expression<Func<UserItemOffer, object>>> ForInactiveOffers()
        => new()
        {
            { nameof(UserItem.Item.Name), o => o.UserItem.Item.Name },
            { nameof(UserItem.Item.Type), o => o.UserItem.Item.Type },
            { "SellerUserName", o => o.Seller.UserName! },
            { nameof(UserItemOffer.Price), o => o.Price },
            { nameof(UserItemOffer.PublishedAt), o => o.PublishedAt },
            { nameof(UserItemOffer.BoughtAt), o => o.BoughtAt! },
            { "BuyerUserName", o => o.Buyer!.UserName! },
        };
}