using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class GeoReplicationSyncScheduler(
    AzureStorageControlPlane controlPlane,
    SubscriptionControlPlane subscriptionControlPlane,
    ITopazLogger logger,
    TimeSpan interval) : ITopazBackgroundService
{
    public string Name => $"Storage — geo-replication sync scheduler (interval: {interval})";
    public DateTimeOffset? ExecutedAt { get; private set; }

    public Task ScanAndUpdateAsync()
    {
        try
        {
            var subscriptionsResult = subscriptionControlPlane.List();
            if (subscriptionsResult.Resource == null)
                return Task.CompletedTask;

            foreach (var subscription in subscriptionsResult.Resource)
            {
                var subscriptionIdentifier = SubscriptionIdentifier.From(Guid.Parse(subscription.SubscriptionId));
                var accountsResult = controlPlane.ListBySubscription(subscriptionIdentifier);
                if (accountsResult.Resource == null) continue;

                foreach (var account in accountsResult.Resource)
                {
                    if (!AzureStorageControlPlane.IsRaGrsSkuName(account.Sku?.Name)) continue;

                    controlPlane.UpdateLastGeoSyncTime(
                        subscriptionIdentifier,
                        account.GetResourceGroup(),
                        account.Name);

                    logger.LogDebug(
                        nameof(GeoReplicationSyncScheduler),
                        nameof(ScanAndUpdateAsync),
                        "Updated LastGeoSyncTime for '{0}'",
                        account.Name);
                }
            }

            ExecutedAt = DateTimeOffset.UtcNow;
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            logger.LogError(nameof(GeoReplicationSyncScheduler), nameof(ScanAndUpdateAsync), exception.Message);
            return Task.FromException(exception);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug(
            nameof(GeoReplicationSyncScheduler),
            nameof(StartAsync),
            "Geo-replication sync scheduler started (interval: {0})",
            interval);

        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await ScanAndUpdateAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown — exit gracefully
        }
    }
}