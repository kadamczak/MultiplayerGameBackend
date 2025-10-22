using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Items.ReadDtos;

namespace MultiplayerGameBackend.Application.Items.Queries.GetItemById;

public class GetItemByIdQueryHandler(ILogger<GetItemByIdQueryHandler> logger,
    IMapper mapper,
    IMultiplayerGameDbContext dbContext) : IRequestHandler<GetItemByIdQuery, ItemReadDto>
{
    public async Task<ItemReadDto> Handle(GetItemByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting Item with id {itemId}", request.Id);
        
        var item = await dbContext
            .Items
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        
        var itemReadDto = mapper.Map<ItemReadDto>(item);
        
        return itemReadDto;
    }
}