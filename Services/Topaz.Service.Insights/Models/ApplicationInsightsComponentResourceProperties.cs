using System.Text.Json.Serialization;

namespace Topaz.Service.Insights.Models;

public sealed class ApplicationInsightsComponentResourceProperties
{
    public string? ApplicationType { get; set; }

    public string? Kind { get; set; }

    public string? FlowType { get; set; }

    public string? RequestSource { get; set; }

    public string? InstrumentationKey { get; set; }

    public string? ConnectionString { get; set; }

    public string ProvisioningState => "Succeeded";

    public string IngestionMode { get; set; } = "LogAnalytics";

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
