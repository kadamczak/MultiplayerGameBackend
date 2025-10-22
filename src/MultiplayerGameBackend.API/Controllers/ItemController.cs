using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Application.Items;
using MultiplayerGameBackend.Application.Items.Responses;
using MultiplayerGameBackend.Application.Items.Requests;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/items")]
public class ItemController(IItemService itemService,
    IValidator<CreateItemDto> createItemDtoValidator,
    IValidator<UpdateItemDto> updateItemDtoValidator) : ControllerBase
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
    public async Task<IActionResult> Create([FromBody] CreateItemDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await createItemDtoValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.ToDictionary());

        var createdId = await itemService.Create(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = createdId }, null);
    }
    
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] int id, UpdateItemDto dto, CancellationToken cancellationToken)
    {
        dto.Id = id;
        
        var validationResult = await updateItemDtoValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.ToDictionary());
        
        await itemService.Update(dto, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var operationCompleted = await itemService.Delete(id, cancellationToken);
        return operationCompleted ? NoContent() : NotFound();
    }

}