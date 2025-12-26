using System.ComponentModel.DataAnnotations;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.UserItemOffers.Requests;

public class CreateUserItemOfferDto
{
    public Guid UserItemId { get; set; }
    
    [Range(UserItemOffer.Constraints.MinPrice,
        UserItemOffer.Constraints.MaxPrice)]
    public int Price  { get; set; }
}