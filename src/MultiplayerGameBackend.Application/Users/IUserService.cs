using MultiplayerGameBackend.Application.Users.Requests;

namespace MultiplayerGameBackend.Application.Users;

public interface IUserService
{
    Task RegisterUser(RegisterDto dto);
    Task AssignUserRole(ModifyUserRoleDto dto, CancellationToken cancellationToken);
    Task UnassignUserRole(ModifyUserRoleDto dto, CancellationToken cancellationToken);
}