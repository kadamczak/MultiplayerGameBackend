using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.FriendRequests.Requests;
using MultiplayerGameBackend.Application.FriendRequests.Responses;

namespace MultiplayerGameBackend.Application.FriendRequests;

public interface IFriendRequestService
{
    Task<Guid> SendFriendRequest(Guid userId, SendFriendRequestDto dto, CancellationToken cancellationToken);
    Task AcceptFriendRequest(Guid userId, Guid requestId, CancellationToken cancellationToken);
    Task RejectFriendRequest(Guid userId, Guid requestId, CancellationToken cancellationToken);
    Task CancelFriendRequest(Guid userId, Guid requestId, CancellationToken cancellationToken);
    Task RemoveFriend(Guid userId, Guid friendUserId, CancellationToken cancellationToken);
    Task<PagedResult<ReadFriendRequestDto>> GetReceivedFriendRequests(Guid userId, GetFriendRequestsDto dto, CancellationToken cancellationToken);
    Task<PagedResult<ReadFriendRequestDto>> GetSentFriendRequests(Guid userId, GetFriendRequestsDto dto, CancellationToken cancellationToken);
    Task<PagedResult<ReadFriendDto>> GetFriends(Guid currentUserId, GetFriendsDto dto, CancellationToken cancellationToken);
}

