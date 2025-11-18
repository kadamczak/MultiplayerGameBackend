namespace MultiplayerGameBackend.Domain.Constants;

public static class ClientTypes
{
    public const string Browser = "Browser";
    public const string Game = "Game";

    public static readonly IReadOnlyList<string> AllClientTypes = [Browser, Game];
    public static bool IsValidClientType(string clientType) => AllClientTypes.Contains(clientType);
}