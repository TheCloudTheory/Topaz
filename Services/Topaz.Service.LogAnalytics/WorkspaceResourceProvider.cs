using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.LogAnalytics;

internal sealed class WorkspaceResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<LogAnalyticsService>(logger)
{
    private const string DataPathFormat = "{0}/{1}/{2}.json";
    private readonly ITopazLogger _logger = logger;
    
    internal string GetDataPath(string tableName, DateTime date, string id) =>
        string.Format(format: DataPathFormat,  tableName, date, id);

    public void SaveIngestedData(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string workspaceName, string data, string dir)
    {
        if (dir.Contains(".."))
            throw new InvalidOperationException("Identifier contains forbidden characters.");
        var path = Path.Combine(GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, workspaceName), dir);
        _logger.LogDebug(nameof(WorkspaceResourceProvider), nameof(SaveIngestedData), "Saving ingested data to {0}.", path);
        
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, data);
    }

    public IEnumerable<string> LoadIngestedData(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string workspaceName,
        string tableName)
    {
        var dataPath = GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, workspaceName);
        var tableDir = Path.Combine(dataPath, tableName);
        if (!Directory.Exists(tableDir))
            return [];
        return Directory.EnumerateFiles(tableDir, "*.json", SearchOption.AllDirectories)
            .Select(File.ReadAllText);
    }
}
