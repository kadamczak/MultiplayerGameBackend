using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Application.Items;
using MultiplayerGameBackend.Application.Items.ReadDtos;
using MultiplayerGameBackend.Application.Items.Requests;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/items")]
public class ItemController(IItemService itemService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReadItemDto?>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await itemService.GetById(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReadItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await itemService.GetAll(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateItemDto dto, CancellationToken cancellationToken)
    {
        var createdId = await itemService.Create(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = createdId }, null);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deletedCount = await itemService.Delete(id, cancellationToken);
        return deletedCount == 0 ? NotFound() : NoContent();
    }

}