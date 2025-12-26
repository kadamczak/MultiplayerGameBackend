using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Localization;
using MultiplayerGameBackend.Infrastructure.Resources;
using System.ComponentModel.DataAnnotations;

namespace MultiplayerGameBackend.API.Providers;

/// <summary>
/// Custom display metadata provider that localizes Data Annotations validation messages
/// </summary>
public class LocalizedValidationMetadataProvider : IValidationMetadataProvider
{
    private readonly IStringLocalizerFactory _localizerFactory;
    
    public LocalizedValidationMetadataProvider(IStringLocalizerFactory localizerFactory)
    {
        _localizerFactory = localizerFactory;
    }
    
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var localizer = _localizerFactory.Create(typeof(SharedResources));

        foreach (var attribute in context.ValidationMetadata.ValidatorMetadata)
        {
            if (attribute is ValidationAttribute validationAttribute)
            {
                if (string.IsNullOrEmpty(validationAttribute.ErrorMessage) && 
                    validationAttribute.ErrorMessageResourceType == null)
                {
                    var errorMessageKey = GetErrorMessageKey(validationAttribute);
                    if (!string.IsNullOrEmpty(errorMessageKey))
                    {
                        validationAttribute.ErrorMessage = localizer[errorMessageKey];
                    }
                }
            }
        }
    }
    
    private static string? GetErrorMessageKey(ValidationAttribute attribute)
    {
        return attribute switch
        {
            RequiredAttribute => "RequiredAttribute_ValidationError",
            EmailAddressAttribute => "EmailAddressAttribute_Invalid",
            StringLengthAttribute => "StringLengthAttribute_ValidationError",
            MaxLengthAttribute => "MaxLengthAttribute_ValidationError",
            RangeAttribute => "RangeAttribute_ValidationError",
            _ => null
        };
    }
}

