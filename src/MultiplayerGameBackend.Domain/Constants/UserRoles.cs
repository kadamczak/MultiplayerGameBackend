namespace MultiplayerGameBackend.Domain.Constants;

public static class UserRoles
{
    public const string User = "User";
    public const string Admin = "Admin";
    
    public static readonly IReadOnlyList<string> AllRoles = [User, Admin];
    public static bool IsValidRole(string role) => AllRoles.Contains(role);
}