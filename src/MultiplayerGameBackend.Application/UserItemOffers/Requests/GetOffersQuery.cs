using MultiplayerGameBackend.Application.Common;

namespace MultiplayerGameBackend.Application.UserItemOffers.Requests;

public class GetOffersQuery
{
    public PagedQuery PagedQuery { get; set; } = new();
    public bool ShowActive { get; set; }
}

