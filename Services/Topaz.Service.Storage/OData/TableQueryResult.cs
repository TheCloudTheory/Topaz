namespace Topaz.Service.Storage.OData;

/// <summary>
/// Result of a table entity query, carrying the matched entities for the current page
/// and optional continuation keys for the next page.
/// </summary>
/// <param name="Entities">The entities to return in the response body.</param>
/// <param name="NextPartitionKey">
/// When non-null, the partition key that the next request should resume from.
/// This value must be surfaced as the <c>x-ms-continuation-NextPartitionKey</c> response header.
/// </param>
/// <param name="NextRowKey">
/// When non-null, the row key that the next request should resume from.
/// This value must be surfaced as the <c>x-ms-continuation-NextRowKey</c> response header.
/// </param>
internal sealed record TableQueryResult(
    object?[] Entities,
    string? NextPartitionKey,
    string? NextRowKey);
