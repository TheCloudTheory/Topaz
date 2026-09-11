using System.Text.Json;
using Topaz.Service.Insights.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Insights;

internal sealed class ApplicationInsightsResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<InsightsService>(logger)
{
    private readonly ITopazLogger _logger = logger;

    private const string TelemetryPathFormat = "{0}/{1}/{2}.json";

    private const string BillingFeaturesFileName = "billingfeatures.json";

    internal string GetTelemetryPath(string tableName, DateTime date, string id) =>
        string.Format(TelemetryPathFormat, tableName, date.ToString("yyyy-MM-dd"), id);

    public void SaveTelemetry(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string componentName,
        string tableName,
        string envelopeJson)
    {
        if (tableName.Contains("..") || tableName.Contains('/') || tableName.Contains('\\'))
            throw new InvalidOperationException("Table name contains forbidden characters.");

        var dataPath = GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, componentName);
        var dir = Path.Combine(dataPath, tableName, DateTime.UtcNow.ToString("yyyy-MM-dd"));
        var file = Path.Combine(dir, $"{Guid.NewGuid()}.json");

        _logger.LogDebug(nameof(ApplicationInsightsResourceProvider), nameof(SaveTelemetry),
            "Saving telemetry to {0}.", file);

        Directory.CreateDirectory(dir);
        File.WriteAllText(file, envelopeJson);
    }

    public IEnumerable<string> LoadTelemetry(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string componentName,
        string? tableName = null)
    {
        var dataPath = GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, componentName);
        var searchRoot = tableName == null
            ? dataPath
            : Path.Combine(dataPath, tableName);

        if (!Directory.Exists(searchRoot))
            return [];

        return Directory.EnumerateFiles(searchRoot, "*.json", SearchOption.AllDirectories)
            .Select(File.ReadAllText);
    }

    public void CreateOrUpdateDataVolumeCap(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string componentName,
        ApplicationInsightsComponentDataVolumeCap dataVolumeCap)
    {
        var file = GetBillingFeaturesPath(subscriptionIdentifier, resourceGroupIdentifier, componentName);

        _logger.LogDebug(nameof(ApplicationInsightsResourceProvider), nameof(CreateOrUpdateDataVolumeCap),
            "Saving data volume cap to {0}.", file);

        File.WriteAllText(file, JsonSerializer.Serialize(dataVolumeCap, GlobalSettings.JsonOptions));
    }

    public ApplicationInsightsComponentDataVolumeCap? GetDataVolumeCap(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string componentName)
    {
        var file = GetBillingFeaturesPath(subscriptionIdentifier, resourceGroupIdentifier, componentName);
        if (!File.Exists(file)) return null;

        var content = File.ReadAllText(file);
        return string.IsNullOrEmpty(content)
            ? null
            : JsonSerializer.Deserialize<ApplicationInsightsComponentDataVolumeCap>(content, GlobalSettings.JsonOptions);
    }

    private string GetBillingFeaturesPath(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string componentName) =>
        Path.Combine(GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier, componentName),
            BillingFeaturesFileName);
}
