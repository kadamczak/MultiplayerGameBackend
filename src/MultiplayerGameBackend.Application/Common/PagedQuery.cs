using MultiplayerGameBackend.Domain.Constants;

namespace MultiplayerGameBackend.Application.Common;

public class PagedQuery
{
    public string? SearchPhrase { get; set; } = null;

    public string? SortBy { get; set; } = null;
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}