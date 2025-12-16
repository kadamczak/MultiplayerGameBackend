using System.ComponentModel.DataAnnotations;

namespace MultiplayerGameBackend.Application.Friends.Requests;

public class SendFriendRequestDto
{
    [Required(ErrorMessage = "Receiver ID is required.")]
    public Guid ReceiverId { get; set; }
}

