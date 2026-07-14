using System.Text.Json.Serialization;

namespace Topaz.Service.Insights.Models;

public sealed class ApplicationInsightsComponentResourceProperties
{
    public string? ApplicationType { get; set; }

    public string? Kind { get; set; }

    public string? FlowType { get; set; }

    public string? RequestSource { get; set; }

    // For some reason, some fields in Application Insights response models
    // are supposed to break convention and instead of using camelCase,
    // their wire path enforces PascalCase (e.g. properties.InstrumentationKey)
    [JsonPropertyName("InstrumentationKey")]
    public string? InstrumentationKey { get; set; }

    [JsonPropertyName("ConnectionString")]
    public string? ConnectionString { get; set; }

    public string ProvisioningState => "Succeeded";

    public string IngestionMode { get; set; } = "LogAnalytics";

    public int RetentionInDays { get; set; } = 90;

    public string PublicNetworkAccessForIngestion { get; set; } = "Enabled";

    public static ApplicationInsightsComponentResourceProperties FromRequest(
        ApplicationInsightsComponentResourceProperties? source,
        string name,
        ushort ingestionPort)
    {
        var key = Guid.NewGuid().ToString();
        var ingestionEndpoint = $"https://{name}.applicationinsights.topaz.local.dev:{ingestionPort}/";
        var liveEndpoint = $"https://{name}.applicationinsights.topaz.local.dev/";
        return new ApplicationInsightsComponentResourceProperties
        {
            ApplicationType = source?.ApplicationType ?? "web",
            Kind = source?.Kind ?? "web",
            FlowType = source?.FlowType ?? "Redfield",
            RequestSource = source?.RequestSource ?? "rest",
            InstrumentationKey = key,
            ConnectionString = $"InstrumentationKey={key};IngestionEndpoint={ingestionEndpoint};LiveEndpoint={liveEndpoint}",
            IngestionMode = source?.IngestionMode ?? "LogAnalytics",
        };
    }
}
