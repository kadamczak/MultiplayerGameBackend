namespace MultiplayerGameBackend.Domain.Constants;

public static class UserRoles
{
    public const string User = "User";
    public const string Admin = "Admin";
    
    public static readonly IReadOnlyList<string> AllRoles = new[] { User, Admin };
}