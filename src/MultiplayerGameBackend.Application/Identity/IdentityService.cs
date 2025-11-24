using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Identity.Requests;
using MultiplayerGameBackend.Application.Identity.Responses;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.Application.Identity;

public class IdentityService(ILogger<IdentityService> logger,
    UserManager<User> userManager,
    IMultiplayerGameDbContext dbContext,
    IConfiguration configuration,
    IUserContext userContext,
    IEmailService emailService) : IIdentityService
{
    public async Task RegisterUser(RegisterDto dto)
    {
        // Check if duplicate username or email
        var userSameName = await userManager.FindByNameAsync(dto.UserName);
        if (userSameName is not null)
            throw new ConflictException(nameof(User),  nameof(User.UserName), "Username", dto.UserName);

        var userSameEmail = await userManager.FindByEmailAsync(dto.Email);
        if (userSameEmail is not null)
            throw new ConflictException(nameof(User), nameof(dto.Email), "Email", dto.Email);

        var user = new User
        {
            UserName = dto.UserName.Trim(),
            Email = dto.Email.Trim(),
            Balance = User.Constraints.StartingBalance
        };

        // Save user and assign "User" role
        var userResult = await userManager.CreateAsync(user, dto.Password);
        if (!userResult.Succeeded)
            throw new ApplicationException(string.Join("; ", userResult.Errors.Select(e => e.Description)));
        
        var roleResult = await userManager.AddToRoleAsync(user, UserRoles.User);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            throw new ApplicationException(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }
        
        // Generate email confirmation token and send confirmation email
        var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await emailService.SendEmailConfirmationAsync(user, user.Email, confirmationToken);
        logger.LogInformation("Email confirmation sent to user {UserId}", user.Id);
    }
    
    public async Task<TokenResponseDto> Login(string clientType, IPAddress ipAddress, LoginDto dto, CancellationToken cancellationToken)
    {
        // Check if username and password are correct
        var user = await userManager.FindByNameAsync(dto.UserName);
        if (user is null)
            throw new NotFoundException(nameof(User), nameof(User.UserName), "Username", dto.UserName);

        var isPasswordValid = await userManager.CheckPasswordAsync(user, dto.Password);
        if (!isPasswordValid)
            throw new ForbidException();
        
        // Check if email is confirmed
        if (!user.EmailConfirmed)
            throw new ForbidException("Email is not confirmed. Please check your email and confirm your account.");
        
        // If client type is "Game", revoke existing refresh tokens for game clients
        if (clientType == ClientTypes.Game)
        {
            await RevokePreviousGameRefreshTokensOfUser(user, cancellationToken);
            // Todo: use signalR to notify other game clients about logout
        }

        // Create access token
        var accessToken = await GenerateAccessToken(user);
        
        // Create and save refresh token in database
        var refreshTokenPlain = GenerateRefreshToken();
        var refreshTokenEntity = CreateRefreshTokenEntity(clientType, ipAddress, user.Id, refreshTokenPlain);

        await dbContext.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Return tokens
        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenPlain,
            ExpiresInSeconds = 900
        };
    }

    public async Task<TokenResponseDto?> Refresh(string clientType, IPAddress ipAddress, string refreshToken,  CancellationToken cancellationToken)
    {
        var refreshTokenHash = RefreshToken.ComputeHash(refreshToken);

        // Find the refresh token in the database
        var storedToken = await dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == refreshTokenHash, cancellationToken);

        // Validate token exists and is active
        if (storedToken is null || storedToken.IsInactive)
            return null;

        var user = storedToken.User ?? throw new InvalidOperationException("Refresh token does not have an associated user.");
        
        // Revoke old refresh token
        storedToken.RevokedAt = DateTime.UtcNow;

        // Generate new access token
        var accessToken = await GenerateAccessToken(user);

        // Create and save new refresh token in database
        var newRefreshTokenPlain = GenerateRefreshToken();
        var newRefreshTokenEntity = CreateRefreshTokenEntity(clientType, ipAddress, user.Id, newRefreshTokenPlain);
        
        await dbContext.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Return new tokens
        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenPlain,
            ExpiresInSeconds = 900
        };
    }

    public async Task Logout(string refreshToken, CancellationToken cancellationToken)
    {
        var refreshTokenHash = RefreshToken.ComputeHash(refreshToken);

        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == refreshTokenHash, cancellationToken);

        if (storedToken is null || storedToken.IsInactive)
            return;

        storedToken.RevokedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    
    public async Task ChangePassword(ChangePasswordDto dto, string refreshToken, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException();
        var userId = Guid.Parse(currentUser.Id);
        
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new NotFoundException(nameof(User), nameof(User.Id), "Id", userId.ToString());
        
        var isCurrentPasswordValid = await userManager.CheckPasswordAsync(user, dto.CurrentPassword);
        if (!isCurrentPasswordValid)
            throw new ForbidException();
        
        var result = await userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            throw new ApplicationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        
        // Revoke all OTHER refresh tokens for this user
        var refreshTokenHash = RefreshToken.ComputeHash(refreshToken);
        var otherRefreshTokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.TokenHash != refreshTokenHash && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);
        
        foreach (var token in otherRefreshTokens)
            token.RevokedAt = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Password changed successfully for user {UserId}. All sessions invalidated.", userId);
    }
    
    public async Task DeleteAccount(DeleteAccountDto dto, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException();
        var userId = Guid.Parse(currentUser.Id);
        
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            throw new NotFoundException(nameof(User), nameof(User.Id), "Id", userId.ToString());
        
        // Verify password before deletion
        var isPasswordValid = await userManager.CheckPasswordAsync(user, dto.Password);
        if (!isPasswordValid)
            throw new ForbidException();
        
        // Delete the user (database constraints will handle related entities)
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new ApplicationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        logger.LogInformation("Account deleted successfully for user {UserId}", userId);
    }
    
    public async Task ForgotPassword(ForgotPasswordDto dto, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        
        if (user is null)
        {
            logger.LogWarning("Password reset requested for non-existent email: {Email}", dto.Email);
            return;
        }
        
        // Generate password reset token
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        
        // Send password reset email
        // In production, you would include a link to your frontend with the token
        // For now, we'll just send the token directly
        await emailService.SendPasswordResetEmailAsync(user, user.Email!, resetToken);
        logger.LogInformation("Password reset email sent to user {UserId}", user.Id);
    }
    
    public async Task ResetPassword(ResetPasswordDto dto, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            throw new NotFoundException(nameof(User), nameof(User.Email), "Email", dto.Email);
        
        var result = await userManager.ResetPasswordAsync(user, dto.ResetToken, dto.NewPassword);
        if (!result.Succeeded)
            throw new ApplicationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        
        // Revoke all refresh tokens for this user
        var refreshTokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);
        
        foreach (var token in refreshTokens)
            token.RevokedAt = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Password reset successfully for user {UserId}. All sessions invalidated.", user.Id);
    }
    
    public async Task ConfirmEmail(ConfirmEmailDto dto, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            throw new NotFoundException(nameof(User), nameof(User.Email), "Email", dto.Email);
        
        if (user.EmailConfirmed)
        {
            logger.LogInformation("Email already confirmed for user {UserId}", user.Id);
            return;
        }
        
        var result = await userManager.ConfirmEmailAsync(user, dto.Token);
        if (!result.Succeeded)
            throw new ApplicationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        
        logger.LogInformation("Email confirmed successfully for user {UserId}", user.Id);
    }
    
    private async Task<string> GenerateAccessToken(User user)
    {
        // Get claims (id, username, email)
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!)
        };
        
        // Get role claims
        var roles = await userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role =>
            new Claim(ClaimTypes.Role, role)));
        
        // Get JWT secret key
        var key = new Microsoft.IdentityModel.Tokens
            .SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("JWT Secret Key is not configured.")));
        
        // Create signing credentials (secret key + hashing algorithm)
        var creds = new Microsoft.IdentityModel.Tokens
            .SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        
        // Create the token
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured."),
            audience: configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured."),
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );
        
        // Serialize JWT access token into a string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    private string GenerateRefreshToken()
        => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
    
    private RefreshToken CreateRefreshTokenEntity(string clientType, IPAddress ipAddress, Guid userId, string refreshTokenPlain)
    {
        var refreshTokenHash = RefreshToken.ComputeHash(refreshTokenPlain);
        
        return new RefreshToken()
        {
            TokenHash = refreshTokenHash,
            DeviceInfo = clientType,
            IpAddress = ipAddress.GetAddressBytes(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };
    }
    
    private async Task RevokePreviousGameRefreshTokensOfUser(User user, CancellationToken cancellationToken)
    {
        var existingGameClientTokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.DeviceInfo == ClientTypes.Game)
            .ToListAsync(cancellationToken);
    
        // For every entity, set RevokedAt to current time
        foreach (var token in existingGameClientTokens)
            token.RevokedAt = DateTime.UtcNow;
    
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Revoked existing game client refresh tokens for user {UserId} on login.", user.Id);
    }
}