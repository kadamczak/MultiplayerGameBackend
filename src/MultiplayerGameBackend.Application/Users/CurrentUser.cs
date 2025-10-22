namespace MultiplayerGameBackend.Application.Users;

public class CurrentUser(string Id,
    string UserName,
    string Email,
    IEnumerable<string> Roles)
{
    public bool IsInRole(string role) => Roles.Contains(role);
}