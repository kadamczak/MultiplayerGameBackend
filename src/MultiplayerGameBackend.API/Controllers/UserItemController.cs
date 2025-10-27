using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Application.UserItems;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/users")]
[Authorize]
public class UserItemController(IUserItemService userItemService) : ControllerBase
{
    [HttpGet("me/items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCurrentUserItems(CancellationToken cancellationToken)
    {
        var userItems = await userItemService.GetCurrentUserItems(cancellationToken);
        return Ok(userItems);
    }
    
    
}