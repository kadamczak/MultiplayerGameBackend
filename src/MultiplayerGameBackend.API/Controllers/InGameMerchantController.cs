using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Application.InGameMerchants;
using MultiplayerGameBackend.Application.InGameMerchants.Responses;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/merchants")]
[Authorize]
public class InGameMerchantController(IInGameMerchantService inGameMerchantService) : ControllerBase
{
    [HttpGet("{merchantId:int}/offers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadMerchantOfferDto?>> GetOffers([FromRoute] int merchantId, CancellationToken cancellationToken)
    {
        var offers = await inGameMerchantService.GetOffers(merchantId, cancellationToken);
        return Ok(offers);
    }
    
    [HttpPost("/offers/{offerId:int}/purchase")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)] // When insufficient funds
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PurchaseOffer([FromRoute] int offerId, CancellationToken cancellationToken)
    {
        await inGameMerchantService.PurchaseOffer(offerId, cancellationToken);
        return NoContent();
    }
}