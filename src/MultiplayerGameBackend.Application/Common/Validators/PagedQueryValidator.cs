using FluentValidation;

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
            .WithMessage("Page number must be at least 1.");
        
        RuleFor(x => x.PageSize)
            .Must(pageSize => pageSizes.Contains(pageSize))
            .WithMessage($"Page size must be one of: {string.Join(", ", pageSizes)}.");
        
        RuleFor(x => x.SortDirection)
            .IsInEnum()
            .WithMessage("Sort direction must be either 'Ascending' or 'Descending'.");

        RuleFor(x => x.SearchPhrase)
            .MaximumLength(maxSearchPhraseLength)
            .When(x => x.SearchPhrase != null)
            .WithMessage($"Search phrase must be at most {maxSearchPhraseLength} characters long.");
        
        if (sortByValues != null && sortByValues.Length > 0)
        {
            RuleFor(x => x.SortBy)
                .Must(sortBy => sortByValues.Contains(sortBy))
                .When(x => x.SortBy != null)
                .WithMessage($"SortBy must be one of: {string.Join(", ", sortByValues)}.");
        }
    }
}

