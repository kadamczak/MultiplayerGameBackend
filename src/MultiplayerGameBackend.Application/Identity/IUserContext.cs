using MultiplayerGameBackend.Application.Users;

namespace MultiplayerGameBackend.Application.Identity;

public interface IUserContext
{
    CurrentUser? GetCurrentUser();
}
