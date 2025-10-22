using FluentValidation.Results;

namespace MultiplayerGameBackend.Application.Extensions;

public static class ValidationExtensions
{
    public static IDictionary<string, string[]> FormatErrors(this ValidationResult validationResult)
    {
        return validationResult.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ErrorMessage).ToArray()
            );
    }
}