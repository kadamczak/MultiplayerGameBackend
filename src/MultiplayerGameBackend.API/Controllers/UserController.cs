using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Application.Extensions;
using MultiplayerGameBackend.Application.Users;
using MultiplayerGameBackend.Application.Users.Requests;
using MultiplayerGameBackend.Application.Users.Requests.Validators;
using MultiplayerGameBackend.Application.Users.Responses;
using MultiplayerGameBackend.Domain.Constants;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/users")]
[Authorize]
public class UserController(
    ModifyUserRoleDtoValidator modifyUserRoleDtoValidator,
    IUserService userService) : ControllerBase
{
    [HttpPost("{id:guid}/roles")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignUserRole(Guid id, ModifyUserRoleDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await modifyUserRoleDtoValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validationResult.FormatErrors()));

        await userService.AssignUserRole(id, dto, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/roles")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnassignUserRole(Guid id, ModifyUserRoleDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await modifyUserRoleDtoValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.ToDictionary());

        await userService.UnassignUserRole(id, dto, cancellationToken);
        return NoContent();
    }

    [HttpGet("me/game-info")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserGameInfoDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserGameInfoDto>> GetCurrentUserGameInfo(CancellationToken cancellationToken)
    {
        var userGameInfo = await userService.GetCurrentUserGameInfo(cancellationToken);
        return Ok(userGameInfo);
    }
    
    [HttpGet("me/customization")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ReadUserCustomizationDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadUserCustomizationDto>> GetUserCustomization(CancellationToken cancellationToken)
    {
        var customization = await userService.GetCurrentUserCustomization(cancellationToken);
        return customization is null ? NotFound() : Ok(customization);
    }

    [HttpPut("me/customization")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUserCustomization(UpdateUserCustomizationDto dto,
        CancellationToken cancellationToken)
    {
        await userService.UpdateUserCustomization(dto, cancellationToken);
        return NoContent();
    }
}