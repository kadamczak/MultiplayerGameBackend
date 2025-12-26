using MultiplayerGameBackend.Application.Common.Validators;
using FluentValidation;
using MultiplayerGameBackend.Application.Common;

namespace MultiplayerGameBackend.Application.Common.Validators;

public class PagedQueryValidator : AbstractValidator<PagedQuery>
{
    public PagedQueryValidator(
        int[]? allowedPageSizes = null,
        string[]? sortByValues = null,
        int maxSearchPhraseLength = 256)
    {
        var pageSizes = allowedPageSizes ?? [5, 10, 15, 20, 25, 50];
        
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage(ValidatorLocalizer.GetString(LocalizationKeys.Validation.InvalidPageNumber));
        
        RuleFor(x => x.PageSize)
            .Must(pageSize => pageSizes.Contains(pageSize))
            .WithMessage(ValidatorLocalizer.GetString(
                LocalizationKeys.Validation.PageSizeMustBeOneOf, 
                string.Join(", ", pageSizes)));
        
        RuleFor(x => x.SearchPhrase)
            .MaximumLength(maxSearchPhraseLength)
            .When(x => x.SearchPhrase != null)
            .WithMessage(ValidatorLocalizer.GetString(
                LocalizationKeys.Validation.SearchPhraseTooLong, 
                maxSearchPhraseLength));
        
        RuleFor(x => x.SortDirection)
            .IsInEnum()
            .WithMessage(ValidatorLocalizer.GetString((LocalizationKeys.Validation.SortDirectionRequired)));
        
        if (sortByValues != null && sortByValues.Length > 0)
        {
            RuleFor(x => x.SortBy)
                .Must(sortBy => sortByValues.Contains(sortBy))
                .When(x => x.SortBy != null)
                .WithMessage(ValidatorLocalizer.GetString(
                    LocalizationKeys.Validation.SortByMustBeOneOf, 
                    string.Join(", ", sortByValues)));
        }
    }
}

