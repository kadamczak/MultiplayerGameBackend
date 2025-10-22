namespace MultiplayerGameBackend.Application.Users.Requests;

public class ModifyUserRoleDto
{
    public required string UserEmail { get; set; }
    public required string RoleName { get; set; }
}