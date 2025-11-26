namespace MultiplayerGameBackend.Domain.Exceptions;

public class PayloadTooLargeException : Exception
{
    public PayloadTooLargeException()
    {
    }
    
    public PayloadTooLargeException(string message) : base(message)
    {
    }
}