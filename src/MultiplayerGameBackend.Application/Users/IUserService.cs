using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Users.Requests;
using MultiplayerGameBackend.Application.Users.Responses;

namespace MultiplayerGameBackend.Application.Users;

public interface IUserService
{
    Task AssignUserRole(Guid userId, ModifyUserRoleDto dto, CancellationToken cancellationToken);
    Task UnassignUserRole(Guid userId, ModifyUserRoleDto dto, CancellationToken cancellationToken);
    Task<UserGameInfoDto> GetCurrentUserGameInfo(Guid userId, bool includeCustomization, bool includeUserItems, CancellationToken cancellationToken);
    Task UpdateUserAppearance(Guid userId, UpdateUserAppearanceDto dto, CancellationToken cancellationToken);
    Task<string> UploadProfilePicture(Guid userId, Stream imageStream, string fileName, CancellationToken cancellationToken);
    Task DeleteProfilePicture(Guid userId, CancellationToken cancellationToken);
    Task<PagedResult<UserSearchResultDto>> SearchFriendableUsers(Guid currentUserId, SearchUsersDto dto, CancellationToken cancellationToken);
}