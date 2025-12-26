using MultiplayerGameBackend.Application.Interfaces;

namespace MultiplayerGameBackend.Application.Common.Validators;

/// <summary>
/// Helper class to provide localized validation messages.
/// Since validators can't easily use DI, we use a static service locator pattern.
/// This is set up during application startup.
/// </summary>
public static class ValidatorLocalizer
{
    private static ILocalizationService? _localizationService;
    
    public static void Initialize(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }
    
    public static string GetString(string key)
    {
        return _localizationService?.GetString(key) ?? key;
    }
    
    public static string GetString(string key, params object[] args)
    {
        return _localizationService?.GetString(key, args) ?? key;
    }
}

