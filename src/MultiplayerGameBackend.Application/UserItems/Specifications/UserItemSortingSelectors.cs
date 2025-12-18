using System.Linq.Expressions;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.UserItems.Specifications;

public class UserItemSortingSelectors
{
    public static Dictionary<string, Expression<Func<UserItem, object>>> ByNameTypeOrDescription()
        => new()
        {
            { nameof(Item.Name), r => r.Item.Name },
            { nameof(Item.Type), r => r.Item.Type },
            { nameof(Item.Description), r => r.Item.Description }
        };
    
    
}