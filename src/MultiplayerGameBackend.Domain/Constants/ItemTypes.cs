namespace MultiplayerGameBackend.Domain.Constants;

public class ItemTypes
{
    public const string EquippableOnHead = "EquippableOnHead";
    public const string EquippableOnBody = "EquippableOnBody";
    public const string Consumable = "Consumable";
    
    public static readonly IReadOnlyList<string> AllItemTypes = [EquippableOnHead, EquippableOnBody, Consumable];
    public static bool IsValidItemType(string clientType) => AllItemTypes.Contains(clientType);
}