using MultiplayerGameBackend.Application.UserItems.Responses;

namespace MultiplayerGameBackend.Application.Users.Responses;

public class UserGameInfoDto
{
    public Guid Id { get; set; }
    public required string UserName { get; set; }
    public int Balance { get; set; }
    public string? ProfilePictureUrl { get; set; }
    
    public ReadUserCustomizationDto? Customization { get; set; }
    public List<ReadUserItemDto>? UserItems { get; set; }
}