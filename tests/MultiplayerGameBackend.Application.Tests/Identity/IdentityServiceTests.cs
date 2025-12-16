using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Identity;
using MultiplayerGameBackend.Application.Identity.Requests;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Tests.TestHelpers;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;
using MultiplayerGameBackend.Tests.Shared.Helpers;
using NSubstitute;

namespace MultiplayerGameBackend.Application.Tests.Identity;

public class IdentityServiceTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly ILogger<IdentityService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public IdentityServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _logger = Substitute.For<ILogger<IdentityService>>();
        _emailService = Substitute.For<IEmailService>();
        
        // Setup configuration for JWT
        var inMemorySettings = new Dictionary<string, string>
        {
            {"Jwt:SecretKey", "ThisIsAVeryLongSecretKeyForTestingPurposesOnly1234567890"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _fixture.CleanDatabase();


    #region RegisterUser Tests

    [Fact]
    public async Task RegisterUser_ShouldCreateUser_WhenValidData()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        // First, create the User role
        var roleManager = IdentityHelper.CreateRoleManager(context);
        await DatabaseHelper.CreateAndSaveRole(roleManager, UserRoles.User);

        var dto = new RegisterDto
        {
            UserName = "newuser",
            Email = "newuser@example.com",
            Password = "Password123!"
        };

        // Act
        await service.RegisterUser(dto);

        // Assert
        var user = await userManager.FindByNameAsync("newuser");
        Assert.NotNull(user);
        Assert.Equal("newuser@example.com", user.Email);
        Assert.Equal(User.Constraints.StartingBalance, user.Balance);
        
        var roles = await userManager.GetRolesAsync(user);
        Assert.Contains(UserRoles.User, roles);

        await _emailService.Received(1).SendEmailConfirmationAsync(
            Arg.Is<User>(u => u.UserName == "newuser"),
            Arg.Is<string>(e => e == "newuser@example.com"),
            Arg.Any<string>()
        );
    }

    [Fact]
    public async Task RegisterUser_ShouldThrowConflictException_WhenUsernameExists()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        await DatabaseHelper.CreateAndSaveUser(userManager, "existinguser", "existing@example.com", "Password123!");

        var dto = new RegisterDto
        {
            UserName = "existinguser",
            Email = "newemail@example.com",
            Password = "Password123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(
            () => service.RegisterUser(dto)
        );
    }

    [Fact]
    public async Task RegisterUser_ShouldThrowConflictException_WhenEmailExists()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        await DatabaseHelper.CreateAndSaveUser(userManager, "existinguser", "existing@example.com", "Password123!");

        var dto = new RegisterDto
        {
            UserName = "newuser",
            Email = "existing@example.com",
            Password = "Password123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(
            () => service.RegisterUser(dto)
        );
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);
        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");

        var dto = new LoginDto
        {
            UserName = "testuser",
            Password = "Password123!"
        };

        // Act
        var result = await service.Login(ClientTypes.Browser, IPAddress.Parse("127.0.0.1"), dto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.Equal(900, result.ExpiresInSeconds);

        // Verify refresh token was saved
        var refreshToken = context.RefreshTokens.FirstOrDefault(rt => rt.UserId == user.Id);
        Assert.NotNull(refreshToken);
    }

    [Fact]
    public async Task Login_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        var dto = new LoginDto
        {
            UserName = "nonexistent",
            Password = "Password123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.Login(ClientTypes.Browser, IPAddress.Parse("127.0.0.1"), dto, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Login_ShouldThrowForbidException_WhenPasswordIsInvalid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");

        var dto = new LoginDto
        {
            UserName = "testuser",
            Password = "WrongPassword!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ForbidException>(
            () => service.Login(ClientTypes.Browser, IPAddress.Parse("127.0.0.1"), dto, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Login_ShouldThrowForbidException_WhenEmailIsNotConfirmed()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");
        user.EmailConfirmed = false; // Not confirmed
        await userManager.UpdateAsync(user);

        var dto = new LoginDto
        {
            UserName = "testuser",
            Password = "Password123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ForbidException>(
            () => service.Login(ClientTypes.Browser, IPAddress.Parse("127.0.0.1"), dto, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Login_ShouldRevokeExistingGameTokens_WhenClientTypeIsGame()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");

        // Create an existing game token
        var existingToken = new RefreshToken
        {
            TokenHash = "oldhash",
            DeviceInfo = ClientTypes.Game,
            IpAddress = IPAddress.Parse("127.0.0.1").GetAddressBytes(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        context.RefreshTokens.Add(existingToken);
        await context.SaveChangesAsync();

        var dto = new LoginDto
        {
            UserName = "testuser",
            Password = "Password123!"
        };

        // Act
        await service.Login(ClientTypes.Game, IPAddress.Parse("127.0.0.1"), dto, CancellationToken.None);

        // Assert
        var revokedToken = context.RefreshTokens.First(rt => rt.TokenHash == "oldhash");
        Assert.NotNull(revokedToken.RevokedAt);
    }

    #endregion

    #region Refresh Tests

    [Fact]
    public async Task Refresh_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");

        // First login to get refresh token
        var loginDto = new LoginDto { UserName = "testuser", Password = "Password123!" };
        var loginResult = await service.Login(ClientTypes.Browser, IPAddress.Parse("127.0.0.1"), loginDto, CancellationToken.None);

        // Act
        await Task.Delay(1000);
        var refreshResult = await service.Refresh(ClientTypes.Browser, IPAddress.Parse("127.0.0.1"), loginResult.RefreshToken, CancellationToken.None);

        // Assert
        Assert.NotNull(refreshResult);
        Assert.NotEmpty(refreshResult.AccessToken);
        Assert.NotEmpty(refreshResult.RefreshToken);
        Assert.NotEqual(loginResult.AccessToken, refreshResult.AccessToken);
        Assert.NotEqual(loginResult.RefreshToken, refreshResult.RefreshToken);
        
        var oldTokenHash = RefreshToken.ComputeHash(loginResult.RefreshToken);
        var newTokenHash = RefreshToken.ComputeHash(refreshResult.RefreshToken);
        
        var oldToken = context.RefreshTokens.First(rt => rt.TokenHash == oldTokenHash);
        Assert.NotNull(oldToken.RevokedAt);
        
        var newToken = context.RefreshTokens.First(rt => rt.TokenHash == newTokenHash);
        Assert.Null(newToken.RevokedAt);
    }

    [Fact]
    public async Task Refresh_ShouldReturnNull_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        // Act
        var result = await service.Refresh(ClientTypes.Browser, IPAddress.Parse("127.0.0.1"), "invalidtoken", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Refresh_ShouldReturnNull_WhenRefreshTokenIsRevoked()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");

        // Login and then logout
        var loginDto = new LoginDto { UserName = "testuser", Password = "Password123!" };
        var loginResult = await service.Login(ClientTypes.Browser, IPAddress.Parse("127.0.0.1"), loginDto, CancellationToken.None);
        await service.Logout(loginResult.RefreshToken, CancellationToken.None);

        // Act
        var refreshResult = await service.Refresh(ClientTypes.Browser, IPAddress.Parse("127.0.0.1"), loginResult.RefreshToken, CancellationToken.None);

        // Assert
        Assert.Null(refreshResult);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_ShouldRevokeRefreshToken_WhenTokenExists()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");

        var loginDto = new LoginDto { UserName = "testuser", Password = "Password123!" };
        var loginResult = await service.Login(ClientTypes.Browser, IPAddress.Parse("127.0.0.1"), loginDto, CancellationToken.None);

        // Act
        await service.Logout(loginResult.RefreshToken, CancellationToken.None);

        // Assert
        var tokenHash = RefreshToken.ComputeHash(loginResult.RefreshToken);
        var token = context.RefreshTokens.First(rt => rt.TokenHash == tokenHash);
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public async Task Logout_ShouldDoNothing_WhenTokenDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        // Act - Should not throw
        await service.Logout("nonexistenttoken", CancellationToken.None);

        // Assert - No exception thrown
        Assert.True(true);
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePassword_ShouldChangePassword_WhenCurrentPasswordIsCorrect()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "OldPassword123!");

        var loginDto = new LoginDto { UserName = "testuser", Password = "OldPassword123!" };
        var loginResult = await service.Login(ClientTypes.Browser, IPAddress.Parse("127.0.0.1"), loginDto, CancellationToken.None);

        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        // Act
        await service.ChangePassword(user.Id, dto, loginResult.RefreshToken, CancellationToken.None);

        // Assert
        var updatedUser = await userManager.FindByIdAsync(user.Id.ToString());
        var isNewPasswordValid = await userManager.CheckPasswordAsync(updatedUser!, "NewPassword123!");
        Assert.True(isNewPasswordValid);
    }

    [Fact]
    public async Task ChangePassword_ShouldRevokeOtherRefreshTokens_ButNotCurrent()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");

        // Create two sessions
        var loginDto = new LoginDto { UserName = "testuser", Password = "Password123!" };
        var session1 = await service.Login(ClientTypes.Browser, IPAddress.Parse("127.0.0.1"), loginDto, CancellationToken.None);
        var session2 = await service.Login(ClientTypes.Browser, IPAddress.Parse("127.0.0.2"), loginDto, CancellationToken.None);

        var dto = new ChangePasswordDto
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword123!"
        };

        // Act - Change password using session1's refresh token
        var user = await userManager.FindByNameAsync("testuser");
        await service.ChangePassword(user!.Id, dto, session1.RefreshToken, CancellationToken.None);

        // Assert
        var session1Hash = RefreshToken.ComputeHash(session1.RefreshToken);
        var session2Hash = RefreshToken.ComputeHash(session2.RefreshToken);

        var session1Token = context.RefreshTokens.First(rt => rt.TokenHash == session1Hash);
        var session2Token = context.RefreshTokens.First(rt => rt.TokenHash == session2Hash);

        Assert.Null(session1Token.RevokedAt); // Current session not revoked
        Assert.NotNull(session2Token.RevokedAt); // Other session revoked
    }

    [Fact]
    public async Task ChangePassword_ShouldThrowForbidException_WhenCurrentPasswordIsWrong()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");

        var loginDto = new LoginDto { UserName = "testuser", Password = "Password123!" };
        var loginResult = await service.Login(ClientTypes.Browser, IPAddress.Parse("127.0.0.1"), loginDto, CancellationToken.None);

        var dto = new ChangePasswordDto
        {
            CurrentPassword = "WrongPassword!",
            NewPassword = "NewPassword123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ForbidException>(
            () => service.ChangePassword(user.Id, dto, loginResult.RefreshToken, CancellationToken.None)
        );
    }

    #endregion

    #region DeleteAccount Tests

    [Fact]
    public async Task DeleteAccount_ShouldDeleteUser_WhenPasswordIsCorrect()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");
        var userId = user.Id;

        var dto = new DeleteAccountDto { Password = "Password123!" };

        // Act
        await service.DeleteAccount(userId, dto, CancellationToken.None);

        // Assert
        var deletedUser = await userManager.FindByIdAsync(userId.ToString());
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task DeleteAccount_ShouldThrowForbidException_WhenPasswordIsWrong()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");

        var dto = new DeleteAccountDto { Password = "WrongPassword!" };

        // Act & Assert
        await Assert.ThrowsAsync<ForbidException>(
            () => service.DeleteAccount(user.Id, dto, CancellationToken.None)
        );
    }

    #endregion

    #region ForgotPassword Tests

    [Fact]
    public async Task ForgotPassword_ShouldSendResetEmail_WhenEmailExists()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");

        var dto = new ForgotPasswordDto { Email = "test@example.com" };

        // Act
        await service.ForgotPassword(dto, CancellationToken.None);

        // Assert
        await _emailService.Received(1).SendPasswordResetEmailAsync(
            Arg.Is<User>(u => u.Email == "test@example.com"),
            Arg.Is<string>(e => e == "test@example.com"),
            Arg.Any<string>()
        );
    }

    [Fact]
    public async Task ForgotPassword_ShouldNotThrow_WhenEmailDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        var dto = new ForgotPasswordDto { Email = "nonexistent@example.com" };

        // Act - Should not throw (security: don't reveal if email exists)
        await service.ForgotPassword(dto, CancellationToken.None);

        // Assert
        await _emailService.DidNotReceive().SendPasswordResetEmailAsync(
            Arg.Any<User>(),
            Arg.Any<string>(),
            Arg.Any<string>()
        );
    }

    #endregion

    #region ResetPassword Tests

    [Fact]
    public async Task ResetPassword_ShouldThrowNotFoundException_WhenEmailDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        var dto = new ResetPasswordDto
        {
            Email = "nonexistent@example.com",
            ResetToken = "sometoken",
            NewPassword = "NewPassword123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.ResetPassword(dto, CancellationToken.None)
        );
    }

    #endregion

    #region ConfirmEmail Tests
    
    [Fact]
    public async Task ConfirmEmail_ShouldNotThrow_WhenEmailAlreadyConfirmed()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        var user = await DatabaseHelper.CreateAndSaveUser(userManager, "testuser", "test@example.com", "Password123!");

        var dto = new ConfirmEmailDto
        {
            Email = "test@example.com",
            Token = "anytoken"
        };

        // Act - Should not throw
        await service.ConfirmEmail(dto, CancellationToken.None);

        // Assert
        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmail_ShouldThrowNotFoundException_WhenEmailDoesNotExist()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var userManager = IdentityHelper.CreateUserManagerWithTokenProvider(context);
        var service = new IdentityService(_logger, userManager, context, _configuration, _emailService);

        var dto = new ConfirmEmailDto
        {
            Email = "nonexistent@example.com",
            Token = "sometoken"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.ConfirmEmail(dto, CancellationToken.None)
        );
    }

    #endregion

}

