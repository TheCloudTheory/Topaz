using Microsoft.AspNetCore.Http;

namespace Topaz.Service.Storage.OData;

/// <summary>
/// Parsed OData query options extracted from an Azure Table Storage request query string.
/// Handles $filter, $select, $top, and the NextPartitionKey/NextRowKey continuation parameters
/// emitted by the Azure Data Tables SDK and the Terraform azurerm provider.
/// </summary>
internal sealed class TableODataQueryOptions
{
    /// <summary>Raw OData filter expression, e.g. "PartitionKey eq 'pk1' and Age gt 30".</summary>
    public string? Filter { get; }

    /// <summary>Property names requested via $select, or null if all properties should be returned.</summary>
    public IReadOnlyList<string>? Select { get; }

    /// <summary>Maximum number of entities to return in one page, or null for no limit.</summary>
    public int? Top { get; }

    /// <summary>Continuation partition key sent by the client in a follow-up paging request.</summary>
    public string? NextPartitionKey { get; }

    /// <summary>Continuation row key sent by the client in a follow-up paging request (may be null).</summary>
    public string? NextRowKey { get; }

    private TableODataQueryOptions(
        string? filter,
        IReadOnlyList<string>? select,
        int? top,
        string? nextPartitionKey,
        string? nextRowKey)
    {
        Filter = filter;
        Select = select;
        Top = top;
        NextPartitionKey = nextPartitionKey;
        NextRowKey = nextRowKey;
    }

    /// <summary>
    /// Parses OData query options from a raw <see cref="QueryString"/>.
    /// Never throws; unknown or malformed parameters are silently ignored.
    /// </summary>
    public static TableODataQueryOptions Parse(QueryString queryString)
    {
        if (!queryString.HasValue)
            return new TableODataQueryOptions(null, null, null, null, null);

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Strip the leading '?' and split into key=value pairs.
        var raw = queryString.Value![1..];
        foreach (var segment in raw.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eqIdx = segment.IndexOf('=');
            if (eqIdx < 0) continue;
            var key = Uri.UnescapeDataString(segment[..eqIdx]);
            // Replace '+' with space — some clients encode spaces this way.
            var val = Uri.UnescapeDataString(segment[(eqIdx + 1)..].Replace('+', ' '));
            dict.TryAdd(key, val);
        }

        var filter = dict.GetValueOrDefault("$filter");

        IReadOnlyList<string>? select = null;
        if (dict.TryGetValue("$select", out var s) && !string.IsNullOrWhiteSpace(s))
            select = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        int? top = null;
        if (dict.TryGetValue("$top", out var t) && int.TryParse(t, out var tv) && tv > 0)
            top = tv;

        var nextPk = dict.GetValueOrDefault("NextPartitionKey");
        var nextRk = dict.GetValueOrDefault("NextRowKey");

        return new TableODataQueryOptions(filter, select, top, nextPk, nextRk);
    }
}
