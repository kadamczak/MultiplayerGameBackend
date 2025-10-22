namespace MultiplayerGameBackend.Application.Items.Requests;

public class CreateItemDto
{
    public required string Name { get; set; }
    public required string Description { get; set; }
}