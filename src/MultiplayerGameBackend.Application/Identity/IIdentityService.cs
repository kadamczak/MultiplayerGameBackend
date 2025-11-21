using System.Net;
using MultiplayerGameBackend.Application.Identity.Requests;
using MultiplayerGameBackend.Application.Identity.Responses;

namespace MultiplayerGameBackend.Application.Identity;

public interface IIdentityService
{
    Task RegisterUser(RegisterDto dto);
    Task<TokenResponseDto> Login(string clientType, IPAddress ipAddress, LoginDto dto, CancellationToken cancellationToken);
    Task<TokenResponseDto?> Refresh(string clientType, IPAddress ipAddress, string refreshToken, CancellationToken cancellationToken);
    Task Logout(string refreshToken, CancellationToken cancellationToken);
    Task ChangePassword(ChangePasswordDto dto);
    Task DeleteAccount(DeleteAccountDto dto);
}