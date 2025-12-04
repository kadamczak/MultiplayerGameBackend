using MultiplayerGameBackend.Application.Users.Requests;
using MultiplayerGameBackend.Application.Users.Responses;

namespace MultiplayerGameBackend.Application.Users;

public interface IUserService
{
    Task AssignUserRole(Guid id, ModifyUserRoleDto dto, CancellationToken cancellationToken);
    Task UnassignUserRole(Guid id, ModifyUserRoleDto dto, CancellationToken cancellationToken);
    Task<UserGameInfoDto> GetCurrentUserGameInfo(bool includeCustomization, bool includeUserItems, CancellationToken cancellationToken);
    Task UpdateUserAppearance(UpdateUserAppearanceDto dto, CancellationToken cancellationToken);
    Task<string> UploadProfilePicture(Stream imageStream, string fileName, CancellationToken cancellationToken);
    Task DeleteProfilePicture(CancellationToken cancellationToken);
}