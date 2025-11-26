namespace MultiplayerGameBackend.Domain.Exceptions;

public class UnsupportedMediaType : Exception
{
    public UnsupportedMediaType()
    {
    }
    
    public UnsupportedMediaType(string message) : base(message)
    {
    }
}