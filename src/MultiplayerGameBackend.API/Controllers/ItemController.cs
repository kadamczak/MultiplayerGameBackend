using MediatR;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Application.Items.Queries.GetItemById;
using MultiplayerGameBackend.Application.Items.ReadDtos;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/items")]
public class ItemController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemReadDto?>> GetById(int id)
    {
        var item = await mediator.Send(new GetItemByIdQuery(id));
        return Ok(item);
    }
}