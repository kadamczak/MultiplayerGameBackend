using MultiplayerGameBackend.Application.Items.Responses;

namespace MultiplayerGameBackend.Application.UserItems.Responses;

public class ReadUserItemDto
{
    public Guid Id { get; set; }
    public required ReadItemDto Item { get; set; }
    
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    
    public Guid? OfferId { get; set; }
}