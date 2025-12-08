using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Domain.Tests.Entities;

public class RefreshTokenTests
{
    #region ComputeHash Tests

    [Fact]
    public void ComputeHash_ShouldReturnBase64String_WithLength44()
    {
        // Arrange
        var token = Guid.NewGuid().ToString();

        // Act
        var hash = RefreshToken.ComputeHash(token);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(44, hash.Length);
        Assert.Matches("^[A-Za-z0-9+/=]+$", hash);
    }

    [Fact]
    public void ComputeHash_ShouldBeDeterministic_SameInputProducesSameOutput()
    {
        // Arrange
        var token = "test-token-12345";

        // Act
        var hash1 = RefreshToken.ComputeHash(token);
        var hash2 = RefreshToken.ComputeHash(token);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_ShouldProduceDifferentHashes_ForDifferentInputs()
    {
        // Arrange
        var token1 = "token-1";
        var token2 = "token-2";

        // Act
        var hash1 = RefreshToken.ComputeHash(token1);
        var hash2 = RefreshToken.ComputeHash(token2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_ShouldHandleEmptyString()
    {
        // Arrange
        var token = string.Empty;

        // Act
        var hash = RefreshToken.ComputeHash(token);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(44, hash.Length);
    }

    [Fact]
    public void ComputeHash_ShouldProduceDifferentHashes_ForSimilarStrings()
    {
        // Arrange
        var token1 = "mytoken";
        var token2 = "mytoken ";
        var token3 = "Mytoken";

        // Act
        var hash1 = RefreshToken.ComputeHash(token1);
        var hash2 = RefreshToken.ComputeHash(token2);
        var hash3 = RefreshToken.ComputeHash(token3);

        // Assert
        Assert.NotEqual(hash1, hash2);
        Assert.NotEqual(hash1, hash3);
        Assert.NotEqual(hash2, hash3);
    }
    
    [Theory]
    [InlineData("token1")]
    [InlineData("very-long-token-string-with-many-characters-12345678901234567890")]
    [InlineData("a")]
    [InlineData("Token!@#$%^&*()")]
    public void ComputeHash_ShouldHandleVariousTokenFormats(string token)
    {
        // Act
        var hash = RefreshToken.ComputeHash(token);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(44, hash.Length);
    }

    #endregion

    #region Verify Tests

    [Fact]
    public void Verify_ShouldReturnTrue_WhenTokenMatches()
    {
        // Arrange
        var token = Guid.NewGuid().ToString();
        var refreshToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeHash(token),
            DeviceInfo = "Test Device",
            IpAddress = new byte[] { 127, 0, 0, 1 },
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var result = refreshToken.Verify(token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenTokenDoesNotMatch()
    {
        // Arrange
        var correctToken = Guid.NewGuid().ToString();
        var wrongToken = Guid.NewGuid().ToString();
        var refreshToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeHash(correctToken),
            DeviceInfo = "Test Device",
            IpAddress = new byte[] { 127, 0, 0, 1 },
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var result = refreshToken.Verify(wrongToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Verify_ShouldBeCaseSensitive()
    {
        // Arrange
        var token = "MyToken123";
        var refreshToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeHash(token),
            DeviceInfo = "Test Device",
            IpAddress = new byte[] { 127, 0, 0, 1 },
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var resultCorrectCase = refreshToken.Verify("MyToken123");
        var resultWrongCase = refreshToken.Verify("mytoken123");

        // Assert
        Assert.True(resultCorrectCase);
        Assert.False(resultWrongCase);
    }

    #endregion

    #region IsExpired Tests

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenExpirationDateIsInPast()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeHash(Guid.NewGuid().ToString()),
            DeviceInfo = "Test Device",
            IpAddress = new byte[] { 127, 0, 0, 1 },
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var result = refreshToken.IsExpired;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenExpirationDateIsInFuture()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeHash(Guid.NewGuid().ToString()),
            DeviceInfo = "Test Device",
            IpAddress = new byte[] { 127, 0, 0, 1 },
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var result = refreshToken.IsExpired;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsRevoked Tests

    [Fact]
    public void IsRevoked_ShouldReturnTrue_WhenRevokedAtHasValue()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeHash(Guid.NewGuid().ToString()),
            DeviceInfo = "Test Device",
            IpAddress = new byte[] { 127, 0, 0, 1 },
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = DateTime.UtcNow
        };

        // Act
        var result = refreshToken.IsRevoked;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRevoked_ShouldReturnFalse_WhenRevokedAtIsNull()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeHash(Guid.NewGuid().ToString()),
            DeviceInfo = "Test Device",
            IpAddress = new byte[] { 127, 0, 0, 1 },
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = null
        };

        // Act
        var result = refreshToken.IsRevoked;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsActive Tests

    [Fact]
    public void IsActive_ShouldReturnTrue_WhenNotExpiredAndNotRevoked()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeHash(Guid.NewGuid().ToString()),
            DeviceInfo = "Test Device",
            IpAddress = new byte[] { 127, 0, 0, 1 },
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = null
        };

        // Act
        var result = refreshToken.IsActive;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsActive_ShouldReturnFalse_WhenExpired()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeHash(Guid.NewGuid().ToString()),
            DeviceInfo = "Test Device",
            IpAddress = new byte[] { 127, 0, 0, 1 },
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = null
        };

        // Act
        var result = refreshToken.IsActive;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsActive_ShouldReturnFalse_WhenRevoked()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeHash(Guid.NewGuid().ToString()),
            DeviceInfo = "Test Device",
            IpAddress = new byte[] { 127, 0, 0, 1 },
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = DateTime.UtcNow
        };

        // Act
        var result = refreshToken.IsActive;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsActive_ShouldReturnFalse_WhenBothExpiredAndRevoked()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeHash(Guid.NewGuid().ToString()),
            DeviceInfo = "Test Device",
            IpAddress = new byte[] { 127, 0, 0, 1 },
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = DateTime.UtcNow
        };

        // Act
        var result = refreshToken.IsActive;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsInactive Tests

    [Fact]
    public void IsInactive_ShouldReturnFalse_WhenActive()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeHash(Guid.NewGuid().ToString()),
            DeviceInfo = "Test Device",
            IpAddress = new byte[] { 127, 0, 0, 1 },
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = null
        };

        // Act
        var result = refreshToken.IsInactive;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsInactive_ShouldReturnTrue_WhenExpired()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeHash(Guid.NewGuid().ToString()),
            DeviceInfo = "Test Device",
            IpAddress = new byte[] { 127, 0, 0, 1 },
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = null
        };

        // Act
        var result = refreshToken.IsInactive;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInactive_ShouldReturnTrue_WhenRevoked()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeHash(Guid.NewGuid().ToString()),
            DeviceInfo = "Test Device",
            IpAddress = new byte[] { 127, 0, 0, 1 },
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = DateTime.UtcNow
        };

        // Act
        var result = refreshToken.IsInactive;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInactive_ShouldBeOppositeOfIsActive()
    {
        // Arrange - Active token
        var activeToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeHash(Guid.NewGuid().ToString()),
            DeviceInfo = "Test Device",
            IpAddress = new byte[] { 127, 0, 0, 1 },
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = null
        };

        // Arrange - Inactive token
        var inactiveToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeHash(Guid.NewGuid().ToString()),
            DeviceInfo = "Test Device",
            IpAddress = new byte[] { 127, 0, 0, 1 },
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = null
        };

        // Act & Assert
        Assert.Equal(!activeToken.IsActive, activeToken.IsInactive);
        Assert.Equal(!inactiveToken.IsActive, inactiveToken.IsInactive);
    }

    #endregion
}

