using FluentValidation;

namespace MultiplayerGameBackend.Application.UserItemOffers.Requests.Validators;

public class GetOffersDtoValidator : AbstractValidator<GetOffersDto>
{
    private static readonly int[] AllowedPageSizes = [5, 10, 15];
    private static readonly string[] ActiveOffersSortByValues = ["Name", "Type", "SellerUserName", "Price", "PublishedAt"];
    private static readonly string[] InactiveOffersSortByValues = ["Name", "Type", "SellerUserName", "Price", "PublishedAt", "BoughtAt", "BuyerUserName"];
    private const int MaxSearchPhraseLength = 256;

    public GetOffersDtoValidator()
    {
        RuleFor(x => x.PagedQuery.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be at least 1.");
        
        RuleFor(x => x.PagedQuery.PageSize)
            .Must(pageSize => AllowedPageSizes.Contains(pageSize))
            .WithMessage($"Page size must be one of: {string.Join(", ", AllowedPageSizes)}.");
        
        RuleFor(x => x.PagedQuery.SearchPhrase)
            .MaximumLength(MaxSearchPhraseLength)
            .When(x => x.PagedQuery.SearchPhrase != null)
            .WithMessage($"Search phrase must be at most {MaxSearchPhraseLength} characters long.");
        
        RuleFor(x => x.PagedQuery.SortDirection)
            .IsInEnum()
            .WithMessage("Sort direction must be either 'Ascending' or 'Descending'.");
        
        RuleFor(x => x.PagedQuery.SortBy)
            .Must((query, sortBy) => ValidateSortBy(sortBy, query.ShowActive))
            .When(x => x.PagedQuery.SortBy != null)
            .WithMessage((query, sortBy) => 
            {
                var validValues = query.ShowActive ? ActiveOffersSortByValues : InactiveOffersSortByValues;
                return $"SortBy must be one of: {string.Join(", ", validValues)}.";
            });
    }

    private bool ValidateSortBy(string? sortBy, bool showActive)
    {
        if (string.IsNullOrEmpty(sortBy))
            return true;

        var validValues = showActive ? ActiveOffersSortByValues : InactiveOffersSortByValues;
        return validValues.Contains(sortBy);
    }
}

