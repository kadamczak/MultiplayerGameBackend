namespace MultiplayerGameBackend.Application.Interfaces;

public interface ILocalizationService
{
    string GetString(string key);
    string GetString(string key, params object[] args);
}

