using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Application.Extensions;
using MultiplayerGameBackend.Application.Identity;
using MultiplayerGameBackend.Application.Identity.Requests;
using MultiplayerGameBackend.Application.Identity.Requests.Validators;
using MultiplayerGameBackend.Application.Identity.Responses;
using MultiplayerGameBackend.Application.Users;
using MultiplayerGameBackend.Application.Users.Requests;
using MultiplayerGameBackend.Application.Users.Requests.Validators;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/identity")]
public class IdentityController(IIdentityService identityService,
    RegisterDtoValidator registerDtoValidator) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await registerDtoValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validationResult.FormatErrors()));
        
        await identityService.RegisterUser(dto);
        return Ok(new { message = "User registered successfully." });
    }
    
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginDto dto)
    {
        var tokens = await identityService.Login(dto);
        return Ok(tokens);
    }
    
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> Refresh([FromBody] string refreshToken)
    {
        var tokens = await identityService.Refresh(refreshToken);
        return tokens is null ? Unauthorized() : Ok(tokens);
    }
    
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] string refreshToken)
    {
        await identityService.Logout(refreshToken);
        return NoContent();
    }
}