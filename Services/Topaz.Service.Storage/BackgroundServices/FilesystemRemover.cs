using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.Storage.BackgroundServices;

internal sealed class FilesystemRemover(
    AzureStorageControlPlane controlPlane,
    SubscriptionControlPlane subscriptionControlPlane,
    ITopazLogger logger) : ITopazBackgroundService
{
    /// <summary>
    /// When a filesystem is deleted, a filesystem with the same identifier cannot be created for at least 30 seconds.
    /// We will achieve this by removing filesystems that have been deleted for at least 30 seconds.
    /// </summary>
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly AdlsServiceControlPlane _adlsServiceControlPlane = AdlsServiceControlPlane.New(logger);

    public string Name => $"ADLS — filesystem remover (interval: {Interval})";
    public DateTimeOffset? ExecutedAt { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug(
            nameof(GeoReplicationSyncScheduler),
            nameof(StartAsync),
            "Geo-replication sync scheduler started (interval: {0})",
            Interval);

        using var timer = new PeriodicTimer(Interval);
        
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await ScanAndDeleteAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown — exit gracefully
        }
    }

    private async Task ScanAndDeleteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var subscriptionsResult = subscriptionControlPlane.List();
            if (subscriptionsResult.Resource == null)
            {
                return;
            }

            foreach (var subscription in subscriptionsResult.Resource)
            {
                var subscriptionIdentifier = SubscriptionIdentifier.From(Guid.Parse(subscription.SubscriptionId));
                var accountsResult = controlPlane.ListBySubscription(subscriptionIdentifier);
                if (accountsResult.Resource == null) continue;

                foreach (var account in accountsResult.Resource)
                {
                    var filesystems = await _adlsServiceControlPlane.ListFilesystems(account, "", cancellationToken);
                    foreach (var filesystem in filesystems.Resource!)
                    {
                        if (filesystem.DeleteScheduled && DateTime.UtcNow > filesystem.ScheduledDeleteDate)
                        {
                            var op = _adlsServiceControlPlane.DeleteFileSystem(account, filesystem.Name!, "");
                            if (op.Result != OperationResult.Deleted)
                            {
                                logger.LogError(nameof(FilesystemRemover), nameof(ScanAndDeleteAsync), $"Failed to delete filesystem {filesystem.Name} in account {account.Name}");
                            }
                        }
                    }
                }
            }

            ExecutedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception exception)
        {
            logger.LogError(nameof(GeoReplicationSyncScheduler), nameof(ScanAndDeleteAsync), exception.Message);
        }
    }
}