using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiplayerGameBackend.API.Services;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Messages;
using MultiplayerGameBackend.Application.Messages.Requests;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("v1/messages")]
public class MessageController(
    IMessageService messageService, 
    IUserContext userContext,
    ILocalizationService localizationService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        var message = await messageService.SendMessage(Guid.Parse(currentUser.Id), dto, cancellationToken);
        return Ok(message);
    }
    
    
    [HttpGet("conversation/{otherUserId:guid}")]
    public async Task<IActionResult> GetConversation(
        [FromRoute] Guid otherUserId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        
        var dto = new GetConversationDto
        {
            OtherUserId = otherUserId,
            PagedQuery = new()
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            }
        };
        
        var result = await messageService.GetConversation(Guid.Parse(currentUser.Id), dto, cancellationToken);
        return Ok(result);
    }
    
    
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        var conversations = await messageService.GetConversations(Guid.Parse(currentUser.Id), cancellationToken);
        return Ok(conversations);
    }


    [HttpPost("read/{otherUserId:guid}")]
    public async Task<IActionResult> MarkMessagesAsRead([FromRoute] Guid otherUserId, CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        await messageService.MarkMessagesAsRead(Guid.Parse(currentUser.Id), otherUserId, cancellationToken);
        return NoContent();
    }


    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        var count = await messageService.GetUnreadCount(Guid.Parse(currentUser.Id), cancellationToken);
        return Ok(new { unreadCount = count });
    }
}