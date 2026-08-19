using Topaz.Service.AppConfiguration.Models;
using Topaz.Service.AppConfiguration.Models.DataPlane;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration;

internal sealed class AppConfigurationDataPlane(AppConfigurationResourceProvider provider,
    ITopazLogger logger)
{
    private const string SnapshotDataSubresource = "snapshots-data";
    
    public static AppConfigurationDataPlane New(AppConfigurationResourceProvider provider, ITopazLogger logger) =>
        new(provider, logger);

    public DataPlaneOperationResult SaveSnapshot(SnapshotSubresource snapshot, AppConfigurationKeyValue[] kvs)
    {
        provider.CreateOrUpdateSubresource(snapshot.GetSubscription(), snapshot.GetResourceGroup(),
            snapshot.Name, snapshot.GetParentId(), SnapshotDataSubresource, kvs);

        return new DataPlaneOperationResult(OperationResult.Success);
    }

    public DataPlaneOperationResult<bool> CanCreateSnapshot(string sku, SnapshotSubresource snapshot)
    {
        var snapshots = provider.ListSubresourcesAs<SnapshotSubresource>(snapshot.GetSubscription(),
            snapshot.GetResourceGroup(), snapshot.GetParentId(), SnapshotDataSubresource);

        if (snapshots.Length == 0)
        {
            return new DataPlaneOperationResult<bool>(OperationResult.Success, true);
        }

        var totalSize = snapshots.Sum(s => s.Properties.Size).GetValueOrDefault() + snapshot.Properties.Size;
        logger.LogDebug(nameof(AppConfigurationDataPlane), nameof(CanCreateSnapshot), "Total size of snapshots: {0}", totalSize);
        
        // App Configuration has a different quota for snapshots depending on the SKU.
        const long oneMegabyte = 1024 * 1024 * 1024;
        var map = new[]
        {
            ("free", oneMegabyte * 10),
            ("developer", oneMegabyte * 500),
            ("standard", oneMegabyte * 1024),
            ("premium", oneMegabyte * 1024 * 4)
        };
        
        var quota = map.Single(item => item.Item1 == sku).Item2;
        if (totalSize > quota)
        {
            return new DataPlaneOperationResult<bool>(OperationResult.Conflict, false,
                "App Configuration quota for snapshots is exceeded.", "QuotaExceeded");
        }
        
        return new DataPlaneOperationResult<bool>(OperationResult.Success, true);
    }
}