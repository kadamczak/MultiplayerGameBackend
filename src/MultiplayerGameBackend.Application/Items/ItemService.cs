using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Common.Mappings;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Items.Responses;
using MultiplayerGameBackend.Application.Items.Requests;
using MultiplayerGameBackend.Domain.Entities;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.Application.Items;

public class ItemService(ILogger<ItemService> logger,
    IMultiplayerGameDbContext dbContext,
    ItemMapper itemMapper,
    ILocalizationService localizationService) : IItemService
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

    public async Task<int> Create(CreateUpdateItemDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating new Item with name {itemName}", dto.Name);
        
        var itemWithSameName = await dbContext
            .Items
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == dto.Name, cancellationToken) ;
        
        if (itemWithSameName is not null)
            throw new ConflictException(nameof(Item), nameof(dto.Name), "Name", dto.Name);
        
        var item = itemMapper.Map(dto);
        
        item.Name = item.Name.Trim();
        item.Description = item.Description.Trim();
        
        await dbContext.Items.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return item.Id;
    }
    
    public async Task Update(int id, CreateUpdateItemDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating Item with id {itemId}", id);
        
        var item = await dbContext.Items.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException(localizationService.GetString(LocalizationKeys.Errors.ItemNotFound));
        
        item.Name = dto.Name.Trim();
        item.Description = dto.Description.Trim();
        item.Type = dto.Type;
        item.ThumbnailUrl = dto.ThumbnailUrl;
        
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<bool> Delete(int id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting Item with id {itemId}", id);
        
        var item = await dbContext.Items.FindAsync([id], cancellationToken);
        
        if (item is null)
        {
            logger.LogWarning("Item with id {itemId} not found", id);
            return false;
        }
    
        dbContext.Items.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
    
}