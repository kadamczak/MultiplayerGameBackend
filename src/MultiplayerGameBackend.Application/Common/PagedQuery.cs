using MultiplayerGameBackend.Domain.Constants;

namespace MultiplayerGameBackend.Application.Common;

public class PagedQuery
{
    public string? SearchPhrase { get; set; }
    
    public string? SortBy { get; set; }
    public SortDirection SortDirection { get; set; }
    
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}