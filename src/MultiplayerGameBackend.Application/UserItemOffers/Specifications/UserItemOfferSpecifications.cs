using System.Linq.Expressions;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.UserItemOffers.Specifications;

public static class UserItemOfferSpecifications
{
    public static Expression<Func<UserItemOffer, bool>> IsActive()
    {
        return o => o.BuyerId == null;
    }
    
    public static Expression<Func<UserItemOffer, bool>> IsInactive()
    {
        return o => o.BuyerId != null;
    }
    
    public static Expression<Func<UserItemOffer, bool>> SearchInActiveOffers(string searchPhraseLower)
    {
        return o => o.UserItem.Item.Name.ToLower().Contains(searchPhraseLower)
                    || o.UserItem.Item.Type.ToLower().Contains(searchPhraseLower)
                    || o.Seller.UserName!.ToLower().Contains(searchPhraseLower);
    }
    
    public static Expression<Func<UserItemOffer, bool>> SearchInInactiveOffers(string searchPhraseLower)
    {
        return o => o.UserItem.Item.Name.ToLower().Contains(searchPhraseLower)
                    || o.UserItem.Item.Type.ToLower().Contains(searchPhraseLower)
                    || o.Seller.UserName!.ToLower().Contains(searchPhraseLower)
                    || o.Buyer!.UserName!.ToLower().Contains(searchPhraseLower);
    }
}