using FluentValidation;

namespace MultiplayerGameBackend.Application.UserItems.Requests.Validators;

public class GetUserItemsDtoValidator : AbstractValidator<GetUserItemsDto>
{
    private static readonly int[] AllowedPageSizes = [5, 10, 15];
    private static readonly string[] SortByValues = ["Name", "Type", "Description"];
    private const int MaxSearchPhraseLength = 256;
    
    public GetUserItemsDtoValidator()
    {
        RuleFor(x => x.PagedQuery.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be at least 1.");
        
        RuleFor(x => x.PagedQuery.PageSize)
            .Must(pageSize => AllowedPageSizes.Contains(pageSize))
            .WithMessage($"Page size must be one of: {string.Join(", ", AllowedPageSizes)}.");
        
        RuleFor(x => x.PagedQuery.SortDirection)
            .IsInEnum()
            .WithMessage("Sort direction must be either 'Ascending' or 'Descending'.");

        RuleFor(x => x.PagedQuery.SearchPhrase)
            .MaximumLength(MaxSearchPhraseLength)
            .When(x => x.PagedQuery.SearchPhrase != null)
            .WithMessage($"Search phrase must be at most {MaxSearchPhraseLength} characters long.");
        
        RuleFor(x => x.PagedQuery.SortBy)
            .Must((query, sortBy) => SortByValues.Contains(sortBy))
            .WithMessage((query, sortBy) => $"SortBy must be one of: {string.Join(", ", SortByValues)}.");
    }
}