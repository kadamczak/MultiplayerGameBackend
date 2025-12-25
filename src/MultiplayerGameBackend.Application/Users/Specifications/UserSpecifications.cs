using System.Linq.Expressions;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Users.Specifications;

public static class UserSpecifications
{
    public static Expression<Func<User, bool>> SearchByUsername(string searchPhraseLower)
    {
        return u => u.UserName!.ToLower().Contains(searchPhraseLower);
    }
}