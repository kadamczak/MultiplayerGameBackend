using MultiplayerGameBackend.Application.Common;

namespace MultiplayerGameBackend.Application.FriendRequests.Requests;

public class GetFriendRequestsDto
{
    public PagedQuery PagedQuery { get; set; } = new();
}