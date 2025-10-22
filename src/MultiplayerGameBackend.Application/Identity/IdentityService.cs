using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Identity.Requests;
using MultiplayerGameBackend.Application.Identity.Responses;
using MultiplayerGameBackend.Application.Users.Requests;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.Application.Identity;

public class IdentityService(ILogger<IdentityService> logger,
    UserManager<User> userManager) : IIdentityService
{
    //private static readonly ConcurrentDictionary<string, Guid> RefreshTokens = new();
    
    public async Task RegisterUser(RegisterDto dto)
    {
        var userSameName = await userManager.FindByNameAsync(dto.UserName);
        if (userSameName is not null)
            throw new ConflictException(nameof(User), dto.UserName);

        var userSameEmail = await userManager.FindByEmailAsync(dto.UserEmail);
        if (userSameEmail is not null)
            throw new ConflictException(nameof(User), dto.UserEmail);

        var user = new User
        {
            UserName = dto.UserName,
            Email = dto.UserEmail
        };

        var createResult = await userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            throw new ApplicationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));

        var roleResult = await userManager.AddToRoleAsync(user, UserRoles.User);
        if (!roleResult.Succeeded)
            throw new ApplicationException(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
    }
    
    public async Task<TokenResponseDto> Login(LoginDto dto)
    {
        var user = await userManager.FindByNameAsync(dto.UserName);
        if (user is null)
            throw new NotFoundException(nameof(User), dto.UserName);

        var isPasswordValid = await userManager.CheckPasswordAsync(user, dto.Password);
        if (!isPasswordValid)
            throw new ForbidException();

        var accessToken = GenerateAccessToken(user);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    
        //RefreshTokens.TryAdd(refreshToken, user.Id);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresInSeconds = 3600
        };
    }

    public Task<TokenResponseDto?> Refresh(string refreshToken)
    {
        throw new NotImplementedException();
    }

    public Task Logout(string refreshToken)
    {
        throw new NotImplementedException();
    }
    
    private string GenerateAccessToken(User user)
    {
        throw new NotImplementedException();
    }
}