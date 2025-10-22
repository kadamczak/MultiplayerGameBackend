using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Common.Mappings;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Items.ReadDtos;

namespace MultiplayerGameBackend.Application.Items;

public class ItemService(ILogger<ItemService> logger,
    IMultiplayerGameDbContext dbContext,
    ItemMapper itemMapper) : IItemService
{
    public async Task<ReadItemDto?> GetById(int id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting Item with id {itemId}", id);
        
        var item = await dbContext
            .Items
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        
        var itemDto = itemMapper.Map(item);
        return itemDto;
    }
    
    public async Task<IEnumerable<ReadItemDto>> GetAll(CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting all items");
        
        var items = await dbContext
            .Items
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        
        return items.Select(itemMapper.Map);
    }
    
}