using MultiplayerGameBackend.Application.Common;

namespace MultiplayerGameBackend.Application.FriendRequests.Requests;

public class GetFriendsDto
{
    public PagedQuery PagedQuery { get; set; } = new();
}