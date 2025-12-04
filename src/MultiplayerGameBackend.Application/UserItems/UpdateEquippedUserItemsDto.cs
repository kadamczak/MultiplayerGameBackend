namespace MultiplayerGameBackend.Application.UserItems;

public class UpdateEquippedUserItemsDto
{
    public Guid? EquippedHeadUserItemId { get; set; }
    public Guid? EquippedBodyUserItemId { get; set; }
}