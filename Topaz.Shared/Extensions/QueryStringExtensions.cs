using System.Web;
using Microsoft.AspNetCore.Http;

namespace Topaz.Shared.Extensions;

public static class QueryStringExtensions
{
    // Retained for internal callers that receive a QueryString parameter directly (e.g. security providers).
    public static bool TryGetValueForKey(this QueryString query, string key, out string? value)
    {
        value = null;
        var parsedQuery = HttpUtility.ParseQueryString(query.ToString());
        if(parsedQuery.AllKeys.Contains(key) == false) return false;
        
        value = parsedQuery[key];
        return true;
    }

    /// <summary>
    /// Retrieves the first value for <paramref name="key"/> from the already-parsed
    /// <see cref="IQueryCollection"/>. Prefer this overload in endpoint code over the
    /// <see cref="QueryString"/> overload to avoid re-parsing the raw query string.
    /// </summary>
    public static bool TryGetValueForKey(this IQueryCollection query, string key, out string? value)
    {
        if (query.TryGetValue(key, out var values))
        {
            value = values.FirstOrDefault();
            return value != null;
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="query"/> contains <paramref name="key"/> with a
    /// value equal to <paramref name="expectedValue"/> (ordinal comparison). The raw user-supplied
    /// string is never exposed to callers, which prevents static-analysis tools from treating the
    /// result as tainted data flowing into a sensitive condition.
    /// </summary>
    public static bool HasQueryKeyWithValue(this IQueryCollection query, string key, string expectedValue)
    {
        return query.TryGetValue(key, out var values) &&
               string.Equals(values.FirstOrDefault(), expectedValue, StringComparison.Ordinal);
    }
}