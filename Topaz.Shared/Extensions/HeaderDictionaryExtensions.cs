using Microsoft.AspNetCore.Http;

namespace Topaz.Shared.Extensions;

public static class HeaderDictionaryExtensions
{
    /// <summary>
    /// Parses HTTP headers from an IHeaderDictionary into a formatted string suitable for logging.
    /// </summary>
    /// <param name="headers">The header dictionary to parse.</param>
    /// <returns>A string containing all headers formatted as "Key: Value" pairs, separated by newlines.</returns>
    public static string ParseHeadersForLogs(this IHeaderDictionary headers)
    {
        return string.Join(Environment.NewLine, headers.Select(x => $"{x.Key}: {x.Value}"));
    }
}