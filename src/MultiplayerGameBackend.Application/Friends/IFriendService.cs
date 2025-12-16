using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Friends.Requests;
using MultiplayerGameBackend.Application.Friends.Responses;

namespace MultiplayerGameBackend.Application.Friends;

public interface IFriendService
{
    Task<Guid> SendFriendRequest(Guid currentUserId, SendFriendRequestDto dto, CancellationToken cancellationToken);
    Task AcceptFriendRequest(Guid currentUserId, Guid requestId, CancellationToken cancellationToken);
    Task RejectFriendRequest(Guid currentUserId, Guid requestId, CancellationToken cancellationToken);
    Task CancelFriendRequest(Guid currentUserId, Guid requestId, CancellationToken cancellationToken);
    Task RemoveFriend(Guid currentUserId, Guid friendUserId, CancellationToken cancellationToken);
    Task<PagedResult<ReadFriendRequestDto>> GetReceivedFriendRequests(Guid currentUserId, PagedQuery query, CancellationToken cancellationToken);
    Task<PagedResult<ReadFriendRequestDto>> GetSentFriendRequests(Guid currentUserId, PagedQuery query, CancellationToken cancellationToken);
    Task<PagedResult<ReadFriendDto>> GetFriends(Guid currentUserId, PagedQuery query, CancellationToken cancellationToken);
}

