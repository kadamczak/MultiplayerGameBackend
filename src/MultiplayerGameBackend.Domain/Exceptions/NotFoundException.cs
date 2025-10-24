namespace MultiplayerGameBackend.Domain.Exceptions;

public class NotFoundException : Exception
{
    public string ResourceType { get; }
    public string PropertyName { get; }
    public string PropertyDisplayName { get; }
    public string IdentifierValue { get; }

    public NotFoundException(string resourceType, string propertyName, string propertyDisplayName, string identifierValue)
        : base($"{resourceType} with this {propertyDisplayName} doesn't exist.")
    {
        ResourceType = resourceType;
        PropertyName = propertyName;
        PropertyDisplayName = propertyDisplayName;
        IdentifierValue = identifierValue;
    }
}