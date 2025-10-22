namespace MultiplayerGameBackend.Domain.Exceptions;

public class ConflictException(string propertyName, string propertyValue) 
    : Exception($"{propertyName} with value: {propertyValue} already exists")
{
    
}