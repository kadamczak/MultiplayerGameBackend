using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.Application.Extensions;
using MultiplayerGameBackend.Application.Identity;
using MultiplayerGameBackend.Application.Identity.Requests;
using MultiplayerGameBackend.Application.Identity.Requests.Validators;
using MultiplayerGameBackend.Application.Identity.Responses;
using MultiplayerGameBackend.Domain.Constants;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/identity")]
public class IdentityController(IIdentityService identityService,
    RegisterDtoValidator registerDtoValidator,
    ChangePasswordDtoValidator changePasswordDtoValidator) : ControllerBase
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
    public async Task<ActionResult<TokenResponseDto>> Login(
        [FromBody] LoginDto dto,
        [FromHeader(Name = "X-Client-Type")] string clientType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientType) || !ClientTypes.IsValidClientType(clientType))
            return BadRequest("Invalid or missing X-Client-Type header.");
        
        if (HttpContext.Connection.RemoteIpAddress is not { } ipAddress)
            return BadRequest("Unable to determine client IP address.");
        
        var tokens = await identityService.Login(clientType, ipAddress, dto, cancellationToken);
        
        // if clientType is "Browser":
        // 1. send refresh token as HttpOnly Secure cookie with SameSite=Strict and Path=/v1/identity/
        // 2. send access token in response body (to be stored in memory by the client)
        if (clientType == ClientTypes.Browser)
        {
            AppendRefreshTokenCookie(tokens.RefreshToken!);
            tokens.RefreshToken = null;
        }
        
        // if clientType is "Game":
        // 1. send refresh token in response body (to be securely stored in a file)
        // 2. send access token in response body (to be securely stored in a file -> loaded to memory)
        
        return Ok(tokens);
    }
    
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> Refresh(
        [FromBody] string? refreshTokenFromBody,
        [FromHeader(Name = "X-Client-Type")] string clientType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientType) || !ClientTypes.IsValidClientType(clientType))
            return BadRequest("Invalid or missing X-Client-Type header.");

        // Get refresh token based on client type
        var tokenToUse = GetRefreshTokenFromClient(refreshTokenFromBody, clientType);

        if (string.IsNullOrWhiteSpace(tokenToUse))
            return BadRequest("Refresh token was not present.");
        
        // Get client IP address
        if (HttpContext.Connection.RemoteIpAddress is not { } ipAddress)
            return BadRequest("Unable to determine client IP address.");

        // Get new token pair
        var tokens = await identityService.Refresh(clientType, ipAddress, tokenToUse, cancellationToken);
        if (tokens is null)
            return Unauthorized();
        
        if (clientType == ClientTypes.Browser)
        {
            AppendRefreshTokenCookie(tokens.RefreshToken!);
            tokens.RefreshToken = null;
        }
        
        return Ok(tokens);
    }
    
    
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromBody] string? refreshToken,
        [FromHeader(Name = "X-Client-Type")] string clientType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientType) || !ClientTypes.IsValidClientType(clientType))
            return BadRequest("Invalid or missing X-Client-Type header.");

        // Get refresh token based on client type
        var tokenToUse = GetRefreshTokenFromClient(refreshToken, clientType);
    
        if (!string.IsNullOrWhiteSpace(tokenToUse))
            await identityService.Logout(tokenToUse, cancellationToken);

        // Clear refresh token cookie for browser clients
        if (clientType == ClientTypes.Browser)
            Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/v1/identity" });

        return NoContent();
    }
    
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await changePasswordDtoValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validationResult.FormatErrors()));
        
        await identityService.ChangePassword(dto);
        return Ok(new { message = "Password changed successfully." });
    }
    
    
    
    private void AppendRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/v1/identity",
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
    
    private string? GetRefreshTokenFromClient(string? refreshTokenFromBody, string clientType)
    {
        if (clientType == ClientTypes.Game) // request body
            return refreshTokenFromBody;
        
        if (clientType == ClientTypes.Browser) // cookie
        {
            if (Request.Cookies.TryGetValue("refreshToken", out var cookieToken))
                return cookieToken;
        }
        return null;
    }
}