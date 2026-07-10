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

    public void SaveIngestedData(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string workspaceId, string data, string dir)
    {
        var path = Path.Combine(GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, workspaceId), dir);
        _logger.LogDebug(nameof(WorkspaceResourceProvider), nameof(SaveIngestedData), "Saving ingested data to {0}.", path);
        
        File.WriteAllText(path, data);
    }
}
