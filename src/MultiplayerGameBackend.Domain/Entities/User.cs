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
        
        public const int ProfilePictureUrlMaxLength = 500;
        public const int ProfilePictureMaxSizeBytes = 2 * 1024 * 1024; // 2 MB
        public const int ProfilePictureCompressedMaxWidth = 500;
        public const int ProfilePictureCompressedMaxHeight = 500;
    }
    
    [Range(Constraints.MinBalance, Constraints.MaxBalance)]
    public int Balance { get; set; } = Constraints.StartingBalance;
    
    [MaxLength(Constraints.ProfilePictureUrlMaxLength)]
    public string? ProfilePictureUrl { get; set; }
    
    public List<RefreshToken> RefreshTokens { get; set; } = [];
    public List<UserItem> UserItems { get; set; } = [];
    public List<UserItemOffer> SoldItemOffers { get; set; } = [];
    public List<UserItemOffer> BoughtItemOffers { get; set; } = [];
    
    public UserCustomization? Customization { get; set; }
}