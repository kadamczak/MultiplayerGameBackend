using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Application.Identity;
using MultiplayerGameBackend.Application.MerchantItemOffers;
using MultiplayerGameBackend.Application.MerchantItemOffers.Responses;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/merchants")]
[Authorize]
public class MerchantItemOfferController(IMerchantItemOfferService merchantItemOfferService,
    IUserContext userContext) : ControllerBase
{
    [HttpGet("{merchantId:int}/offers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadMerchantOfferDto?>> GetOffers([FromRoute] int merchantId, CancellationToken cancellationToken)
    {
        var offers = await merchantItemOfferService.GetOffers(merchantId, cancellationToken);
        return Ok(offers);
    }
    
    [HttpPost("offers/{offerId:int}/purchase")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)] // When insufficient funds
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PurchaseOffer([FromRoute] int offerId, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException("User must be authenticated.");
        var userId = Guid.Parse(currentUser.Id);
        
        await merchantItemOfferService.PurchaseOffer(userId, offerId, cancellationToken);
        return NoContent();
    }
}