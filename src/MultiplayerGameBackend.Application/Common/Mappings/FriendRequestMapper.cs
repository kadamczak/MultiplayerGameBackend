using MultiplayerGameBackend.Application.FriendRequests.Responses;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Common.Mappings;

public class FriendRequestMapper
{
    public ReadFriendRequestDto? Map(FriendRequest? friendRequest)
    {
        if (friendRequest is null)
            return null;

        return new ReadFriendRequestDto
        {
            Id = friendRequest.Id,
            RequesterId = friendRequest.RequesterId,
            RequesterUsername = friendRequest.Requester.UserName!,
            RequesterProfilePictureUrl = friendRequest.Requester.ProfilePictureUrl,
            ReceiverId = friendRequest.ReceiverId,
            ReceiverUsername = friendRequest.Receiver.UserName!,
            ReceiverProfilePictureUrl = friendRequest.Receiver.ProfilePictureUrl,
            Status = friendRequest.Status,
            CreatedAt = friendRequest.CreatedAt,
            RespondedAt = friendRequest.RespondedAt
        };
    }
}