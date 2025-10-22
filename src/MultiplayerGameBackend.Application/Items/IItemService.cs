using MultiplayerGameBackend.Application.Items.ReadDtos;

namespace MultiplayerGameBackend.Application.Items;

public interface IItemService
{
    Task<ItemReadDto?> GetById(int id, CancellationToken cancellationToken);
}