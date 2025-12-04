namespace MultiplayerGameBackend.Application.Users.Requests;

public class UpdateUserEquippedItemsDto
{
    public Guid? HeadUserItemId { get; set; }
    public Guid? BodyUserItemId { get; set; }
}