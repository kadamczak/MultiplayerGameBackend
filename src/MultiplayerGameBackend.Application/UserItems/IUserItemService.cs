using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.UserItems.Requests;
using MultiplayerGameBackend.Application.UserItems.Responses;

namespace MultiplayerGameBackend.Application.UserItems;

public interface IUserItemService
{
    Task<PagedResult<ReadUserItemDto>> GetUserItems(Guid userId, GetUserItemsDto dto, CancellationToken cancellationToken);
    Task UpdateEquippedUserItems(Guid userId, UpdateEquippedUserItemsDto dto, CancellationToken cancellationToken);
}