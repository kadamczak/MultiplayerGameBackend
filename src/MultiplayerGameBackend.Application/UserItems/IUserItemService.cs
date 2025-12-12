using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.UserItems.Requests;
using MultiplayerGameBackend.Application.UserItems.Responses;

namespace MultiplayerGameBackend.Application.UserItems;

public interface IUserItemService
{
    Task<PagedResult<ReadUserItemDto>> GetCurrentUserItems(Guid userId, PagedQuery query, CancellationToken cancellationToken);
    Task UpdateEquippedUserItems(Guid userId, UpdateEquippedUserItemsDto dto, CancellationToken cancellationToken);
}