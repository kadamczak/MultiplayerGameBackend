using System.Linq.Expressions;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.UserItemOffers.Specifications;

public static class UserItemOfferSortingSelectors
{
    public static Dictionary<string, Expression<Func<UserItemOffer, object>>> ForActiveOffers = new()
    {
        { nameof(UserItem.Item.Name), r => r.UserItem.Item.Name },
        { nameof(UserItem.Item.Type), r => r.UserItem.Item.Type },
        { "SellerUserName", r => r.Seller.UserName },
        { nameof(UserItemOffer.Price), r => r.Price },
        { nameof(UserItemOffer.PublishedAt), r => r.PublishedAt },
    };
    
    public static Dictionary<string, Expression<Func<UserItemOffer, object>>> ForInactiveOffers = new()
    {
        { nameof(UserItem.Item.Name), r => r.UserItem.Item.Name },
        { nameof(UserItem.Item.Type), r => r.UserItem.Item.Type },
        { "SellerUserName", r => r.Seller.UserName },
        { nameof(UserItemOffer.Price), r => r.Price },
        { nameof(UserItemOffer.PublishedAt), r => r.PublishedAt },
        { nameof(UserItemOffer.BoughtAt), r => r.BoughtAt },
        { "BuyerUserName", r => r.Buyer.UserName },
    };
}