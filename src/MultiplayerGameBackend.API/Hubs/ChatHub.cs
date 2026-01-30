using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MultiplayerGameBackend.API.Services;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Interfaces;
using MultiplayerGameBackend.Application.Messages;
using MultiplayerGameBackend.Application.Messages.Requests;
using MultiplayerGameBackend.Domain.Exceptions;

namespace MultiplayerGameBackend.API.Hubs;

[Authorize]
public class ChatHub(
    IMessageService messageService, 
    IUserContext userContext,
    ILocalizationService localizationService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        var currentUserId = Guid.Parse(currentUser.Id);
        
        var unreadCount = await messageService.GetUnreadCount(currentUserId, CancellationToken.None);
        await Clients.Caller.SendAsync("UnreadCount", unreadCount);
        
        await base.OnConnectedAsync();
    }
    
    public async Task SendMessage(SendMessageDto dto)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        var currentUserId = Guid.Parse(currentUser.Id);
        
        var message = await messageService.SendMessage(currentUserId, dto, CancellationToken.None);
        await Clients.User(dto.ReceiverId.ToString()).SendAsync("ReceiveMessage", message);
        await Clients.Caller.SendAsync("MessageSent", message);
    }

    public async Task MarkMessagesAsRead(Guid otherUserId)
    {
        var currentUser = userContext.GetCurrentUser() ?? throw new ForbidException(localizationService.GetString(LocalizationKeys.Errors.UserMustBeAuthenticated));
        var currentUserId = Guid.Parse(currentUser.Id);
        
        await messageService.MarkMessagesAsRead(currentUserId, otherUserId, CancellationToken.None);
        await Clients.User(otherUserId.ToString()).SendAsync("MessagesMarkedAsRead", currentUserId);
    }
}

