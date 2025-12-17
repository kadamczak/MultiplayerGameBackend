using MultiplayerGameBackend.Application.FriendRequests.Responses;
using MultiplayerGameBackend.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace MultiplayerGameBackend.Application.Common.Mappings;

[Mapper]
public partial class FriendRequestMapper
{
    public partial ReadFriendRequestDto? Map(FriendRequest? friendRequest);
}