namespace MultiplayerGameBackend.Domain.Exceptions;

public class NotFoundException(string resourceType, string identifier, string identifierValue) 
    : Exception($"{resourceType} with {identifier}: {identifierValue} doesn't exist.")
{
}