using MultiplayerGameBackend.Application.Identity.Requests;
using MultiplayerGameBackend.Application.Identity.Responses;
using MultiplayerGameBackend.Application.Users.Requests;

namespace MultiplayerGameBackend.Application.Identity;

public interface IIdentityService
{
    Task RegisterUser(RegisterDto dto);
    Task<TokenResponseDto> Login(LoginDto dto);
    Task<TokenResponseDto?> Refresh(string refreshToken);
    Task Logout(string refreshToken);
}