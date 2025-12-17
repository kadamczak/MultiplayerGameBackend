using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MultiplayerGameBackend.Domain.Constants;

namespace MultiplayerGameBackend.Application.Common;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplySearchFilter<T>(
        this IQueryable<T> query,
        string? searchPhrase,
        Expression<Func<T, bool>> searchPredicate)
    {
        if (string.IsNullOrWhiteSpace(searchPhrase))
            return query;

        return query.Where(searchPredicate);
    }

    public static IQueryable<T> ApplySorting<T>(
        this IQueryable<T> query,
        string? sortBy,
        SortDirection sortDirection,
        Dictionary<string, Expression<Func<T, object>>> columnSelectors,
        Expression<Func<T, object>>? defaultSort = null)
    {
        Expression<Func<T, object>>? sortExpression = null;
        
        if (!string.IsNullOrWhiteSpace(sortBy))
            sortExpression = columnSelectors.GetValueOrDefault(sortBy);

        sortExpression ??= defaultSort;
        
        if (sortExpression is not null)
        {
            return sortDirection == SortDirection.Ascending
                ? query.OrderBy(sortExpression)
                : query.OrderByDescending(sortExpression);
        }

        return query;
    }
    
    public static IQueryable<T> ApplyPaging<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize)
    {
        return query
            .Skip(pageSize * (pageNumber - 1))
            .Take(pageSize);
    }
    
    public static IQueryable<T> ApplyPaging<T>(
        this IQueryable<T> query,
        PagedQuery pagedQuery)
    {
        return query.ApplyPaging(pagedQuery.PageNumber, pagedQuery.PageSize);
    }
    
    public static async Task<PagedResult<TResult>> ToPagedResultAsync<TSource, TResult>(
        this IQueryable<TSource> query,
        Expression<Func<TSource, TResult>> selector,
        PagedQuery pagedQuery,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .ApplyPaging(pagedQuery)
            .Select(selector)
            .ToListAsync(cancellationToken);

        return new PagedResult<TResult>(items, totalCount, pagedQuery.PageSize, pagedQuery.PageNumber);
    }
    
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PagedQuery pagedQuery,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .ApplyPaging(pagedQuery)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, pagedQuery.PageSize, pagedQuery.PageNumber);
    }
}

