namespace MultiplayerGameBackend.API.Tests.Helpers;

/// <summary>
/// Helper class to match the PagedResult structure returned from the API
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalItemsCount { get; set; }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}

