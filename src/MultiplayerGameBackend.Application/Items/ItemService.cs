using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Common.Mappings;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Items.ReadDtos;
using MultiplayerGameBackend.Application.Items.Requests;

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

    public Task<int> Create(CreateItemDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating new Item with name {itemName}", dto.Name);
        
        var item = itemMapper.Map(dto);
        dbContext.Items.Add(item);
        return dbContext.SaveChangesAsync(cancellationToken);
    }
    
    public Task<int> Delete(int id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting Item with id {itemId}", id);
        
        var item = dbContext.Items.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            logger.LogWarning("Item with id {itemId} not found", id);
            return Task.FromResult(0);
        }
        
        dbContext.Items.Remove(item);
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}