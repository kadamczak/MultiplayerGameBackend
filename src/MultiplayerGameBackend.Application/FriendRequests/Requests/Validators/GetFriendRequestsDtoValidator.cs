using FluentValidation;
using MultiplayerGameBackend.Application.Common;
using MultiplayerGameBackend.Application.Common.Validators;

namespace MultiplayerGameBackend.Application.FriendRequests.Requests.Validators;

public class GetFriendRequestsDtoValidator : AbstractValidator<GetFriendRequestsDto>
{
    private static readonly int[] AllowedPageSizes = [2, 3, 5, 10, 15, 20];
    private static readonly string[] SortByValues = ["CreatedAt", "UserName"];
    private const int MaxSearchPhraseLength = 100;
    
    public GetFriendRequestsDtoValidator()
    {
        RuleFor(x => x.PagedQuery)
            .SetValidator(new PagedQueryValidator(
                allowedPageSizes: AllowedPageSizes,
                sortByValues: SortByValues,
                maxSearchPhraseLength: MaxSearchPhraseLength));
    }
}