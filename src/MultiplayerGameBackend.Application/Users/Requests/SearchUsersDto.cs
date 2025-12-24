using MultiplayerGameBackend.Application.Common;
namespace MultiplayerGameBackend.Application.Users.Requests;
public class SearchUsersDto
{
    public PagedQuery PagedQuery { get; set; } = new();
}



