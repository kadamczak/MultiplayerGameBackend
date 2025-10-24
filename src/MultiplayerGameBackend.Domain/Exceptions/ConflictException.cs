namespace MultiplayerGameBackend.Domain.Exceptions;

public class ConflictException : Exception
{
    public Dictionary<string, string[]>? Errors { get; }

    // Original constructor for simple conflicts
    public ConflictException(string entityName, string propertyName, string displayName, string propertyValue) 
        : base($"{entityName} with {propertyName}: '{propertyValue}' already exists.")
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { $"{entityName} with this {displayName} already exists." } }
        };
    }

    // Constructor for custom field-level errors
    public ConflictException(Dictionary<string, string[]> errors) 
        : base("Conflict occurred.")
    {
        Errors = errors;
    }
}