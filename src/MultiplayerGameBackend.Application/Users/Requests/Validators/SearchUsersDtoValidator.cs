using FluentValidation;
using MultiplayerGameBackend.Application.Common.Validators;

namespace MultiplayerGameBackend.Application.Users.Requests.Validators;

public class SearchUsersDtoValidator : AbstractValidator<SearchUsersDto>
{
    private static readonly int[] AllowedPageSizes = [5, 10, 15, 20, 25];
    private static readonly string[] SortByValues = ["UserName"];
    private const int MaxSearchPhraseLength = 100;
    
    public SearchUsersDtoValidator()
    {
        RuleFor(x => x.PagedQuery)
            .SetValidator(new PagedQueryValidator(
                allowedPageSizes: AllowedPageSizes,
                sortByValues: SortByValues,
                maxSearchPhraseLength: MaxSearchPhraseLength));
    }
}

