using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Application.Extensions;
using MultiplayerGameBackend.Application.Users;
using MultiplayerGameBackend.Application.Users.Requests;
using MultiplayerGameBackend.Application.Users.Requests.Validators;
using MultiplayerGameBackend.Application.Users.Responses;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Exceptions;

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
    public async Task<ActionResult<UserGameInfoDto>> GetCurrentUserGameInfo(
        [FromQuery] bool includeCustomization = false, 
        [FromQuery] bool includeUserItems = false,
        CancellationToken cancellationToken = default)
    {
        var userGameInfo = await userService.GetCurrentUserGameInfo(includeCustomization, includeUserItems, cancellationToken);
        return Ok(userGameInfo);
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

    [HttpPost("me/profile-picture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file, CancellationToken cancellationToken)
    {
        // Validate file size
        if (file.Length == 0)
            throw new BadRequest("No file uploaded.");

        if (file.Length > Domain.Entities.User.Constraints.ProfilePictureMaxSizeBytes)
            throw new PayloadTooLargeException("Max file size is 2 MB." );

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
            throw new UnsupportedMediaType("Only JPG, JPEG, and PNG files are allowed.");

        await using var stream = file.OpenReadStream();
        var profilePictureUrl = await userService.UploadProfilePicture(stream, file.FileName, cancellationToken);
        
        return Ok(new { profilePictureUrl });
    }

    [HttpDelete("me/profile-picture")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProfilePicture(CancellationToken cancellationToken)
    {
        await userService.DeleteProfilePicture(cancellationToken);
        return NoContent();
    }
}