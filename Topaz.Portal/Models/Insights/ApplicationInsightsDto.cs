namespace Topaz.Portal.Models.Insights;

public sealed class ApplicationInsightsDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Location { get; init; }
    public string? ResourceGroupName { get; init; }
    public string? SubscriptionId { get; init; }
    public string? SubscriptionName { get; init; }
    public string? Kind { get; init; }
    public string? ApplicationType { get; init; }
    public string? InstrumentationKey { get; init; }
    public string? ConnectionString { get; init; }
    public string? IngestionMode { get; init; }
    public int RetentionInDays { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ListApplicationInsightsResponse
{
    public ApplicationInsightsDto[] Value { get; init; } = [];
}
