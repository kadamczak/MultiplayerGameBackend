using MultiplayerGameBackend.Application.Users.Responses;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Common.Mappings;

public class UserMapper
{
    public UserSearchResultDto MapToSearchResultDto(User user)
    {
        return new UserSearchResultDto
        {
            Id = user.Id,
            UserName = user.UserName!,
            ProfilePictureUrl = user.ProfilePictureUrl
        };
    }
}