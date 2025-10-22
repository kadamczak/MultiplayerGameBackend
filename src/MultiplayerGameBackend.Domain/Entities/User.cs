using Microsoft.AspNetCore.Identity;

namespace MultiplayerGameBackend.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public const int UserNameMaxLength = 30;
    public const int EmailMaxLength = 100;
    public const int PasswordHashMaxLength = 512;
    public const int RawPasswordMinLength = 6;
    
    public List<UserItem> UserItems { get; set; } = [];
}