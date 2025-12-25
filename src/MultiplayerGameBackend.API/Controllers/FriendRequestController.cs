using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.API.Services;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Extensions;
using MultiplayerGameBackend.Application.FriendRequests;
using MultiplayerGameBackend.Application.FriendRequests.Requests;
using MultiplayerGameBackend.Application.FriendRequests.Requests.Validators;
using MultiplayerGameBackend.Application.FriendRequests.Responses;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.API.Controllers;

[ApiController]
[Route("v1/friends")]
[Authorize]
public class FriendRequestController(
    IFriendRequestService friendRequestService,
    IUserContext userContext,
    ILocalizationService localizationService,
    GetFriendRequestsDtoValidator getFriendRequestsDtoValidator,
    GetFriendsDtoValidator getFriendsDtoValidator) : ControllerBase
{
    [HttpPost("requests")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendFriendRequest(SendFriendRequestDto dto, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        var requestId = await friendRequestService.SendFriendRequest(Guid.Parse(currentUser.Id), dto, cancellationToken);
        return CreatedAtAction(nameof(SendFriendRequest), new { id = requestId }, null);
    }

    [HttpPost("requests/{requestId:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptFriendRequest(Guid requestId, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        await friendRequestService.AcceptFriendRequest(Guid.Parse(currentUser.Id), requestId, cancellationToken);
        return NoContent();
    }

    [HttpPost("requests/{requestId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectFriendRequest(Guid requestId, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        await friendRequestService.RejectFriendRequest(Guid.Parse(currentUser.Id), requestId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("requests/{requestId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelFriendRequest(Guid requestId, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        await friendRequestService.CancelFriendRequest(Guid.Parse(currentUser.Id), requestId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{friendUserId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFriend(Guid friendUserId, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        await friendRequestService.RemoveFriend(Guid.Parse(currentUser.Id), friendUserId, cancellationToken);
        return NoContent();
    }

    [HttpGet("requests/received")]
    [ProducesResponseType(typeof(PagedResult<ReadFriendRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReceivedFriendRequests([FromQuery] GetFriendRequestsDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await getFriendRequestsDtoValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validationResult.FormatErrors()));
        
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        var result = await friendRequestService.GetReceivedFriendRequests(Guid.Parse(currentUser.Id), dto, cancellationToken);
        return Ok(result);
    }

    [HttpGet("requests/sent")]
    [ProducesResponseType(typeof(PagedResult<ReadFriendRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSentFriendRequests([FromQuery] GetFriendRequestsDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await getFriendRequestsDtoValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validationResult.FormatErrors()));
        
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        var result = await friendRequestService.GetSentFriendRequests(Guid.Parse(currentUser.Id), dto, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReadFriendDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFriends([FromQuery] GetFriendsDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await getFriendsDtoValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validationResult.FormatErrors()));
        
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        var result = await friendRequestService.GetFriends(Guid.Parse(currentUser.Id), dto, cancellationToken);
        return Ok(result);
    }
}

