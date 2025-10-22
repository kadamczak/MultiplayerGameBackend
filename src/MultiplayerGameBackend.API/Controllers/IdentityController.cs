using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Application.Users;
using MultiplayerGameBackend.Application.Users.Requests;
using MultiplayerGameBackend.Application.Users.Requests.Validators;
using MultiplayerGameBackend.Domain.Constants;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/identity")]
public class IdentityController(IUserService userService,
    ModifyUserRoleDtoValidator modifyUserRoleDtoValidator,
    RegisterDtoValidator registerDtoValidator,
    UserManager<User> userManager) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await registerDtoValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.ToDictionary());
        
        await userService.RegisterUser(dto);
        return Ok(new { message = "User registered successfully." });
    }
    
    
    [HttpPost("roles")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignUserRole(ModifyUserRoleDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await modifyUserRoleDtoValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.ToDictionary());
        
        await userService.AssignUserRole(dto, cancellationToken);
        return NoContent();
    }

    [HttpDelete("roles")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnassignUserRole(ModifyUserRoleDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await modifyUserRoleDtoValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.ToDictionary());
        
        await userService.UnassignUserRole(dto, cancellationToken);
        return NoContent();
    }
}