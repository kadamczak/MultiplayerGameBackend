namespace MultiplayerGameBackend.Domain.Exceptions;

public class UnprocessableEntityException : Exception
{
    public Dictionary<string, string[]>? Errors { get; }
    
    public UnprocessableEntityException(string message) 
        : base(message)
    {
    }
    
    public UnprocessableEntityException(Dictionary<string, string[]> errors) 
        : base("Logic error.")
    {
        Errors = errors;
    }
}

