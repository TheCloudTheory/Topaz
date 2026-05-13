using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

internal sealed class KeyVaultSoftDeletePurgeScheduler(
    KeyVaultControlPlane controlPlane,
    SubscriptionControlPlane subscriptionControlPlane,
    ITopazLogger logger,
    TimeSpan interval)
{
    public Task ScanAndPurgeAsync()
    {
        try
        {
            var subscriptionsResult = subscriptionControlPlane.List();
            if (subscriptionsResult.Resource == null)
            {
                return Task.CompletedTask;
            }

            foreach (var subscription in subscriptionsResult.Resource)
            {
                var subscriptionIdentifier = SubscriptionIdentifier.From(Guid.Parse(subscription.SubscriptionId));
                var (_, deletedVaults) = controlPlane.ListDeletedBySubscription(subscriptionIdentifier);

                if (deletedVaults == null) continue;

                foreach (var vault in deletedVaults)
                {
                    if (vault?.ScheduledPurgeDate == null || vault.ScheduledPurgeDate > DateTimeOffset.UtcNow)
                        continue;

                    var (result, _) = controlPlane.Purge(subscriptionIdentifier, vault.Location ?? string.Empty, vault.Name);
                    if (result == OperationResult.Success)
                    {
                        logger.LogDebug(
                            nameof(KeyVaultSoftDeletePurgeScheduler),
                            nameof(ScanAndPurgeAsync),
                            "Purged expired soft-deleted Key Vault '{0}' in subscription '{1}'",
                            vault.Name,
                            subscription.SubscriptionId);
                    }
                }
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug(
            nameof(KeyVaultSoftDeletePurgeScheduler),
            nameof(StartAsync),
            "Soft-delete purge scheduler started (interval: {0})",
            interval);

        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await ScanAndPurgeAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown — exit gracefully
        }
    }
}
