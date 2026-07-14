namespace Topaz.Service.Insights.Models;

internal sealed class UpdateComponentRequest
{
    public IDictionary<string, string>? Tags { get; set; }
    public UpdateComponentRequestProperties? Properties { get; set; }
}

internal sealed class UpdateComponentRequestProperties
{
    public int? RetentionInDays { get; set; }
    public string? PublicNetworkAccessForIngestion { get; set; }
}
