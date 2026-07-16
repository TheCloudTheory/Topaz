using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Insights;

internal sealed class ApplicationInsightsResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<InsightsService>(logger)
{
    private readonly ITopazLogger _logger = logger;

    private const string TelemetryPathFormat = "{0}/{1}/{2}.json";

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
}
