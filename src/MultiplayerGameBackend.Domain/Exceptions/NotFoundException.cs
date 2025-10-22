namespace MultiplayerGameBackend.Domain.Exceptions;

public class NotFoundException(string resourceType, int resourceIdentifier) 
    : Exception($"{resourceType} with id: {resourceIdentifier} doesn't exist")
{
}