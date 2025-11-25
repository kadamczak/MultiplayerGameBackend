using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.UserItems.Responses;

namespace MultiplayerGameBackend.Application.UserItems;

public interface IUserItemService
{
    Task<PagedResult<ReadUserItemDto>> GetCurrentUserItems(PagedQuery query, CancellationToken cancellationToken);
}