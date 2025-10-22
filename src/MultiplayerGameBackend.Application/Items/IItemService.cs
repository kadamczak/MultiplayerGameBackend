using MultiplayerGameBackend.Application.Items.ReadDtos;
using MultiplayerGameBackend.Application.Items.Requests;

namespace MultiplayerGameBackend.Application.Items;

public interface IItemService
{
    Task<ReadItemDto?> GetById(int id, CancellationToken cancellationToken);
    Task<IEnumerable<ReadItemDto>> GetAll(CancellationToken cancellationToken);
    Task<int> Create(CreateItemDto dto, CancellationToken cancellationToken);
    Task<int> Delete(int id, CancellationToken cancellationToken);
}