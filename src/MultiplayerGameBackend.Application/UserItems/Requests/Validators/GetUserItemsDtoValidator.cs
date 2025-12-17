using FluentValidation;
using MultiplayerGameBackend.Application.Common.Validators;

namespace MultiplayerGameBackend.Application.UserItems.Requests.Validators;

public class GetUserItemsDtoValidator : AbstractValidator<GetUserItemsDto>
{
    private static readonly int[] AllowedPageSizes = [5, 10, 15];
    private static readonly string[] SortByValues = ["Name", "Type", "Description"];
    private const int MaxSearchPhraseLength = 256;
    
    public GetUserItemsDtoValidator()
    {
        RuleFor(x => x.PagedQuery)
            .SetValidator(new PagedQueryValidator(
                allowedPageSizes: AllowedPageSizes,
                sortByValues: SortByValues,
                maxSearchPhraseLength: MaxSearchPhraseLength));
    }
}