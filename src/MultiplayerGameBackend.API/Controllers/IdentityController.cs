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
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
    {
        var (clientType, clientTypeError) = GetClientType();
        if (clientTypeError is not null || clientType is null)
            return BadRequest(clientTypeError);
        
        if (HttpContext.Connection.RemoteIpAddress is not { } ipAddress)
            return BadRequest("Unable to determine client IP address.");
        
        var tokens = await identityService.Login(clientType, ipAddress, dto, cancellationToken);
        
        // if clientType is "Browser":
        // 1. send refresh token as HttpOnly Secure cookie with SameSite=Strict and Path=/v1/identity/refresh
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
    public async Task<ActionResult<TokenResponseDto>> Refresh(string? refreshTokenFromBody, CancellationToken cancellationToken)
    {
        // Get client type from header
        var (clientType, clientTypeError) = GetClientType();
        if (clientTypeError is not null || clientType is null)
            return BadRequest(clientTypeError);

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

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(string? refreshToken, CancellationToken cancellationToken)
    {
        // Get client type from header
        var (clientType, clientTypeError) = GetClientType();
        if (clientTypeError is not null || clientType is null)
            return BadRequest(clientTypeError);

        // Get refresh token based on client type
        var tokenToUse = GetRefreshTokenFromClient(refreshToken, clientType);
    
        if (!string.IsNullOrWhiteSpace(tokenToUse))
            await identityService.Logout(tokenToUse, cancellationToken);

        // Clear refresh token cookie for browser clients
        if (clientType == ClientTypes.Browser)
        {
            Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/v1/identity/refresh" });
        }

        return NoContent();
    }
    
    private (string? clientType, IActionResult? error) GetClientType()
    {
        if (!Request.Headers.TryGetValue("X-Client-Type", out var clientTypeValues))
            return (null, BadRequest("X-Client-Type header is required."));
        
        if (clientTypeValues.Count > 1)
            return (null, BadRequest("Multiple X-Client-Type header values are not allowed."));

        var clientType = clientTypeValues[0];

        if (string.IsNullOrWhiteSpace(clientType))
            return (null, BadRequest("X-Client-Type header value cannot be empty."));

        if (!ClientTypes.IsValidClientType(clientType))
            return (null, BadRequest("Invalid X-Client-Type header value."));

        return (clientType, null);
    }
    
    private void AppendRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/v1/identity/refresh",
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