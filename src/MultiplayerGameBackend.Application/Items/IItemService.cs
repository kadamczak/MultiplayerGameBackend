using MultiplayerGameBackend.Application.Items.Responses;
using MultiplayerGameBackend.Application.Items.Requests;

namespace MultiplayerGameBackend.Application.Items;

public interface IItemService
{
    Task<ReadItemDto?> GetById(int id, CancellationToken cancellationToken);
    Task<IEnumerable<ReadItemDto>> GetAll(CancellationToken cancellationToken);
    Task<int> Create(CreateUpdateItemDto dto, CancellationToken cancellationToken);
    Task Update(int id, CreateUpdateItemDto dto, CancellationToken cancellationToken);
    Task<bool> Delete(int id, CancellationToken cancellationToken);
}