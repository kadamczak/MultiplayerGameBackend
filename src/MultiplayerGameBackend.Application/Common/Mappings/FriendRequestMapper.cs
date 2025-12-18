using MultiplayerGameBackend.Application.FriendRequests.Responses;
using MultiplayerGameBackend.Application.Friends.Responses;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.Common.Mappings;

public class FriendRequestMapper
{
    public ReadFriendRequestDto? MapToReadFriendRequestDto(FriendRequest? friendRequest)
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
    
    public ReadFriendDto? MapToReadFriendDto(FriendRequest? friendRequest, Guid currentUserId)
    {
        if (friendRequest is null)
            return null;

        var isFriendTheRequester = friendRequest.RequesterId != currentUserId;

        return new ReadFriendDto
        {
            UserId = isFriendTheRequester ? friendRequest.RequesterId : friendRequest.ReceiverId,
            Username = isFriendTheRequester ? friendRequest.Requester.UserName! : friendRequest.Receiver.UserName!,
            ProfilePictureUrl = isFriendTheRequester ? friendRequest.Requester.ProfilePictureUrl : friendRequest.Receiver.ProfilePictureUrl,
            FriendsSince = friendRequest.RespondedAt ?? friendRequest.CreatedAt
        };
    }
}