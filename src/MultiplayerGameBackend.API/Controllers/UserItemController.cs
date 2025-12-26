using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.API.Services;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Extensions;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.UserItems;
using MultiplayerGameBackend.Application.UserItems.Requests;
using MultiplayerGameBackend.Application.UserItems.Requests.Validators;
using MultiplayerGameBackend.Application.UserItems.Responses;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/users")]
[Authorize]
public class UserItemController(
    IUserItemService userItemService,
    GetUserItemsDtoValidator getUserItemsDtoValidator,
    IUserContext userContext,
    ILocalizationService localizationService) : ControllerBase
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
        
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        var userId = Guid.Parse(currentUser.Id);
        
        var userItems = await userItemService.GetUserItems(userId, dto, cancellationToken);
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
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        var userId = Guid.Parse(currentUser.Id);
        
        await userItemService.UpdateEquippedUserItems(userId, dto, cancellationToken);
        return NoContent();
    }
    
}