namespace MultiplayerGameBackend.Domain.Constants;

public class FriendRequestStatuses
{
    public const string Pending = "Pending";
    public const string Accepted = "Accepted";
    
    public static readonly IReadOnlyList<string> AllStatuses = [Pending, Accepted];
    public static bool IsValidStatus(string status) => AllStatuses.Contains(status);
}

