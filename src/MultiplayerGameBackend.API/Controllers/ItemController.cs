using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Application.Items;
using MultiplayerGameBackend.Application.Items.ReadDtos;
using MultiplayerGameBackend.Application.Items.Requests;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/items")]
public class ItemController(IItemService itemService,
    IValidator<CreateItemDto> createItemDtoValidator) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadItemDto?>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await itemService.GetById(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }
    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReadItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await itemService.GetAll(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)] // In case of duplicate item name
    public async Task<ActionResult<int>> Create([FromBody] CreateItemDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await createItemDtoValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.ToDictionary());

        var createdId = await itemService.Create(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = createdId }, null);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var operationCompleted = await itemService.Delete(id, cancellationToken);
        return operationCompleted ? NoContent() : NotFound();
    }

}