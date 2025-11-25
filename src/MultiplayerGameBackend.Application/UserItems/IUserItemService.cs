using MultiplayerGameBackend.Application.UserItems.Responses;

namespace MultiplayerGameBackend.Application.UserItems;

public interface IUserItemService
{
    Task<IEnumerable<ReadUserItemDto>> GetCurrentUserItems(CancellationToken cancellationToken);
}