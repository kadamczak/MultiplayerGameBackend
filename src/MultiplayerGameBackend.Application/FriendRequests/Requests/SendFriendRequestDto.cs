using System.ComponentModel.DataAnnotations;

namespace MultiplayerGameBackend.Application.FriendRequests.Requests;

public class SendFriendRequestDto
{
    [Required]
    public Guid ReceiverId { get; set; }
}