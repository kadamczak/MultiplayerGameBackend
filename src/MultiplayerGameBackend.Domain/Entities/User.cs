using System.ComponentModel.DataAnnotations;
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
        
        public const int StartingBalance = 300;
        public const int MinBalance = 0;
        public const int MaxBalance = 999_999;
    }
    
    [Range(Constraints.MinBalance, Constraints.MaxBalance)]
    public int Balance { get; set; } = Constraints.StartingBalance;
    
    public List<RefreshToken> RefreshTokens { get; set; } = [];
    public List<UserItem> UserItems { get; set; } = [];

    public int? CustomizationId { get; set; }
    public UserCustomization? Customization { get; set; }
}