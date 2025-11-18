namespace MultiplayerGameBackend.Application.UserItemOffers.Requests;

public class CreateUserItemOfferDto
{
    public Guid UserItemId { get; set; }
    public int Price  { get; set; }
}