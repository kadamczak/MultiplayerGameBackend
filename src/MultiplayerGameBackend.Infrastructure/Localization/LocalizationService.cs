using MultiplayerGameBackend.Application.Interfaces;
using Microsoft.Extensions.Localization;

namespace MultiplayerGameBackend.Infrastructure.Localization;
public class LocalizationService(IStringLocalizer<SharedResources> localizer) : ILocalizationService
{
    public string GetString(string key) => localizer[key];
    public string GetString(string key, params object[] args) => localizer[key, args];
}



