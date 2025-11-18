namespace MultiplayerGameBackend.Application.Items.Responses;

public class ReadItemDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Type { get; set; }
}