using MultiplayerGameBackend.Application.Items.ReadDtos;

namespace MultiplayerGameBackend.Application.Items;

public interface IItemService
{
    Task<ReadItemDto?> GetById(int id, CancellationToken cancellationToken);
    Task<IEnumerable<ReadItemDto>> GetAll(CancellationToken cancellationToken);
}