using FluentValidation;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Common.Validators;

namespace MultiplayerGameBackend.Application.UserItemOffers.Requests.Validators;

public class GetOffersDtoValidator : AbstractValidator<GetOffersDto>
{
    private static readonly int[] AllowedPageSizes = [5, 10, 15];
    private static readonly string[] ActiveOffersSortByValues = ["Name", "Type", "SellerUserName", "Price", "PublishedAt"];
    private static readonly string[] InactiveOffersSortByValues = ["Name", "Type", "SellerUserName", "Price", "PublishedAt", "BoughtAt", "BuyerUserName"];
    private const int MaxSearchPhraseLength = 256;

    public GetOffersDtoValidator()
    {
        RuleFor(x => x.PagedQuery)
            .SetValidator(new PagedQueryValidator(
                allowedPageSizes: AllowedPageSizes,
                sortByValues: null, // Custom validation below
                maxSearchPhraseLength: MaxSearchPhraseLength));
        
        RuleFor(x => x.PagedQuery.SortBy)
            .Must((query, sortBy) => ValidateSortBy(sortBy, query.ShowActive))
            .When(x => x.PagedQuery.SortBy != null)
            .WithMessage((query, sortBy) => 
            {
                var validValues = query.ShowActive ? ActiveOffersSortByValues : InactiveOffersSortByValues;
                return ValidatorLocalizer.GetString(
                    LocalizationKeys.Validation.SortByMustBeOneOf, 
                    string.Join(", ", validValues));
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

