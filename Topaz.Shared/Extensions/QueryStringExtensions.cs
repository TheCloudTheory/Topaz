using System.Web;
using Microsoft.AspNetCore.Http;

namespace Topaz.Shared.Extensions;

public static class QueryStringExtensions
{
    public static bool TryGetValueForKey(this QueryString query, string key, out string? value)
    {
        value = null;
        var parsedQuery = HttpUtility.ParseQueryString(query.ToString());
        if(parsedQuery.AllKeys.Contains(key) == false) return false;
        
        value = parsedQuery[key];
        return true;
    }
}