using MultiplayerGameBackend.Domain.Constants;

namespace MultiplayerGameBackend.Application.Common;

public class PagedQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchPhrase { get; set; } = null;
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
    public string? SortBy { get; set; } = null;
}