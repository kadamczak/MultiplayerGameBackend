namespace MultiplayerGameBackend.Application.Users.Responses;

public class ReadUserCustomizationDto
{
    public required string BodyColor { get; set; }
    public required string EyeColor { get; set; }
    public required string WingColor { get; set; }
    public required string HornColor { get; set; }
    public required string MarkingsColor { get; set; }
    
    public int WingType { get; set; }
    public int HornType { get; set; }
    public int MarkingsType { get; set;}
    
    public Guid UserId { get; set; }
}