namespace MultiplayerGameBackend.Application.Items.ReadDtos;

public class ItemReadDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
}