using Microsoft.AspNetCore.Identity;

namespace MultiplayerGameBackend.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public static class Constraints
    {
        // username - unique
        public const int UserNameMinLength = 2;
        public const int UserNameMaxLength = 30;
        
        // email - unique
        public const int EmailMaxLength = 100;
        
        public const int PasswordHashMaxLength = 512;
        public const int RawPasswordMinLength = 6;
        public const int RawPasswordMaxLength = 256;
    }
    
    public List<UserItem> UserItems { get; set; } = [];
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}