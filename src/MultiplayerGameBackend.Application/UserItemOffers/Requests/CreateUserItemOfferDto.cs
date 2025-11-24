using System.ComponentModel.DataAnnotations;
using MultiplayerGameBackend.Domain.Entities;

namespace MultiplayerGameBackend.Application.UserItemOffers.Requests;

public class CreateUserItemOfferDto
{
    public Guid UserItemId { get; set; }
    
    [Range(UserItemOffer.Constraints.MinPrice,
        UserItemOffer.Constraints.MaxPrice,
        ErrorMessage = "Price must be in {1} to {2} range.")]
    public int Price  { get; set; }
}