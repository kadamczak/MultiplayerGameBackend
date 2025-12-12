namespace MultiplayerGameBackend.Application.UserItems.Requests;

public class UpdateEquippedUserItemsDto
{
    public Guid? EquippedHeadUserItemId { get; set; }
    public Guid? EquippedBodyUserItemId { get; set; }
}