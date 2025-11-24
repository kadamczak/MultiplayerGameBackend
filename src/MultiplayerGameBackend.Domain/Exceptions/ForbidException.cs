namespace MultiplayerGameBackend.Domain.Exceptions;

public class ForbidException : Exception
{
    public ForbidException()
    {
    }
    
    public ForbidException(string message) : base(message)
    {
    }
}
