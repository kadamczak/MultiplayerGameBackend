using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace MultiplayerGameBackend.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // unique
    [Required]
    [StringLength(44, MinimumLength = 44)]
    public required string TokenHash { get; set; }
    
    [Required]
    [StringLength(20)]
    public required string DeviceInfo { get; set; }
    
    [MaxLength(16)] // IPv6 = 16 bytes
    [MinLength(4)]
    public required byte[] IpAddress { get; set; }
    
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsExpired && !IsRevoked;
    public bool IsInactive => !IsActive;
    
    
    public static string ComputeHash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
    
    public bool Verify(string token) => TokenHash == ComputeHash(token);
}