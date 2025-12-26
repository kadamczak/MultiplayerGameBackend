using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.API.Services;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Extensions;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.UserItemOffers;
using MultiplayerGameBackend.Application.UserItemOffers.Requests;
using MultiplayerGameBackend.Application.UserItemOffers.Requests.Validators;
using MultiplayerGameBackend.Application.UserItemOffers.Responses;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/users")]
[Authorize]
public class UserItemOfferController(
    IUserItemOfferService userItemOfferService,
    GetOffersDtoValidator getOffersDtoValidator,
    IUserContext userContext,
    ILocalizationService localizationService) : ControllerBase
{
    [HttpGet("offers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReadUserItemOfferDto?>> GetOffers([FromQuery] GetOffersDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await getOffersDtoValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validationResult.FormatErrors()));
        
        var offers = await userItemOfferService.GetOffers(dto, cancellationToken);
        return Ok(offers);
    }

    [HttpPost("offers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)] // in case offer already exists for this useritem
    public async Task<IActionResult> CreateOffer([FromBody] CreateUserItemOfferDto dto, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        var userId = Guid.Parse(currentUser.Id);
        
        await userItemOfferService.CreateOffer(userId, dto, cancellationToken);
        return NoContent();
    }
    
    [HttpDelete("offers/{offerId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOffer([FromRoute] Guid offerId, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        var userId = Guid.Parse(currentUser.Id);
        
        await userItemOfferService.DeleteOffer(userId, offerId, cancellationToken);
        return NoContent();
    }
    
    [HttpPost("offers/{offerId:guid}/purchase")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)] // When insufficient funds
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PurchaseOffer([FromRoute] Guid offerId, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        var buyerId = Guid.Parse(currentUser.Id);
        
        await userItemOfferService.PurchaseOffer(buyerId, offerId, cancellationToken);
        return NoContent();
    }
}