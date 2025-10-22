using MultiplayerGameBackend.Application.Users.Requests;

namespace MultiplayerGameBackend.Application.Users;

public interface IUserService
{
    Task AssignUserRole(Guid id, ModifyUserRoleDto dto, CancellationToken cancellationToken);
    Task UnassignUserRole(Guid id, ModifyUserRoleDto dto, CancellationToken cancellationToken);
}