namespace MultiplayerGameBackend.Application.Items.Requests;

public class UpdateItemDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}