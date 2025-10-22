using Microsoft.AspNetCore.Identity;

namespace MultiplayerGameBackend.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public List<UserItem> UserItems { get; set; } = [];
}