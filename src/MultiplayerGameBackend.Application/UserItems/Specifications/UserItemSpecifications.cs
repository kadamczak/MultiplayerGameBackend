using System.Linq.Expressions;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.UserItems.Specifications;

public static class UserItemSpecifications
{
    public static Expression<Func<UserItem, bool>> SearchByNameTypeOrDescription(string searchPhraseLower)
    {
        return ui => ui.Item.Name.ToLower().Contains(searchPhraseLower)
                     || ui.Item.Type.ToLower().Contains(searchPhraseLower)
                     || ui.Item.Description.ToLower().Contains(searchPhraseLower);
    }
    
}