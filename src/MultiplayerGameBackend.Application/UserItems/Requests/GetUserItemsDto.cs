using MultiplayerGameBackend.Application.Common;

namespace MultiplayerGameBackend.Application.UserItems.Requests;

public class GetUserItemsDto
{
    public PagedQuery PagedQuery { get; set; } = new();
}