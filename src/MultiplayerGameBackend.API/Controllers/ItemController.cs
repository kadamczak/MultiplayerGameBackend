using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Application.Items;
using MultiplayerGameBackend.Application.Items.ReadDtos;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/items")]
public class ItemController(IItemService itemService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemReadDto?>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await itemService.GetById(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }
}