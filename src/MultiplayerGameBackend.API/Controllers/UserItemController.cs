using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Application.Extensions;
using MultiplayerGameBackend.Application.UserItems;
using MultiplayerGameBackend.Application.UserItems.Requests;
using MultiplayerGameBackend.Application.UserItems.Requests.Validators;
using MultiplayerGameBackend.Application.UserItems.Responses;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/users")]
[Authorize]
public class UserItemController(IUserItemService userItemService,
    GetUserItemsDtoValidator getUserItemsDtoValidator) : ControllerBase
{
    [HttpGet("me/items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ReadUserItemDto>>> GetCurrentUserItems([FromQuery] GetUserItemsDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await getUserItemsDtoValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validationResult.FormatErrors()));
        
        var userItems = await userItemService.GetCurrentUserItems(dto.PagedQuery, cancellationToken);
        return Ok(userItems);
    }
    
    [HttpPut("me/equipped-items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateEquippedUserItems([FromBody] UpdateEquippedUserItemsDto dto,
        CancellationToken cancellationToken)
    {
        await userItemService.UpdateEquippedUserItems(dto, cancellationToken);
        return NoContent();
    }
    
}