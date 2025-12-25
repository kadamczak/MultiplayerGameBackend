using System.Linq.Expressions;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Users.Specifications;

public class UserSortingSelectors
{
    public static Dictionary<string, Expression<Func<User, object>>> ByUserName()
        => new()
        {
            { nameof(User.UserName), u => u.UserName! }
        };
}