using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Application.UserItemOffers;
using MultiplayerGameBackend.Application.UserItemOffers.Requests;
using MultiplayerGameBackend.Application.UserItemOffers.Responses;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/users")]
[Authorize]
public class UserItemOfferController(IUserItemOfferService userItemOfferService) : ControllerBase
{
    [HttpGet("offers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReadActiveUserItemOfferDto?>> GetActiveOffers(CancellationToken cancellationToken)
    {
        var offers = await userItemOfferService.GetActiveOffers(cancellationToken);
        return Ok(offers);
    }

    [HttpPost("offers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)] // in case offer already exists for this useritem
    public async Task<IActionResult> CreateOffer([FromBody] CreateUserItemOfferDto dto, CancellationToken cancellationToken)
    {
        await userItemOfferService.CreateOffer(dto, cancellationToken);
        return NoContent();
    }
    
    [HttpDelete("offers/{offerId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOffer([FromRoute] Guid offerId, CancellationToken cancellationToken)
    {
        await userItemOfferService.DeleteOffer(offerId, cancellationToken);
        return NoContent();
    }
    
    [HttpPost("offers/{offerId:guid}/purchase")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)] // When insufficient funds
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PurchaseOffer([FromRoute] Guid offerId, CancellationToken cancellationToken)
    {
        await userItemOfferService.PurchaseOffer(offerId, cancellationToken);
        return NoContent();
    }
}