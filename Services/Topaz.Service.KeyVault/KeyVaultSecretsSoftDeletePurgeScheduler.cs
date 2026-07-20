using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

internal sealed class KeyVaultSecretsSoftDeletePurgeScheduler(
    KeyVaultControlPlane controlPlane,
    KeyVaultSecretsDataPlane dataPlane,
    SubscriptionControlPlane subscriptionControlPlane,
    ITopazLogger logger,
    TimeSpan interval) : ITopazBackgroundService
{
    public string Name => $"Key Vault — soft-deleted secret purge (interval: {interval})";
    public DateTimeOffset? ExecutedAt { get; private set; }

    public Task ScanAndPurgeAsync()
    {
        try
        {
            var subscriptionsResult = subscriptionControlPlane.List();
            if (subscriptionsResult.Resource == null)
                return Task.CompletedTask;

            foreach (var subscription in subscriptionsResult.Resource)
            {
                var subscriptionIdentifier = SubscriptionIdentifier.From(Guid.Parse(subscription.SubscriptionId));
                var vaultsResult = controlPlane.ListBySubscription(subscriptionIdentifier);
                if (vaultsResult.Resource == null) continue;

                foreach (var vault in vaultsResult.Resource)
                {
                    var resourceGroupIdentifier = vault.GetResourceGroup();
                    var deletedResult = dataPlane.GetDeletedSecrets(
                        subscriptionIdentifier, resourceGroupIdentifier, vault.Name);
                    if (deletedResult.Resource == null) continue;

                    foreach (var record in deletedResult.Resource)
                    {
                        if (record.Secret?.Name == null) continue;

                        var purgeDate = DateTimeOffset.FromUnixTimeSeconds(record.ScheduledPurgeDate);
                        if (purgeDate > DateTimeOffset.UtcNow) continue;

                        var result = dataPlane.PurgeDeletedSecret(
                            subscriptionIdentifier, resourceGroupIdentifier, vault.Name, record.Secret.Name);
                        if (result.Result == OperationResult.Deleted)
                        {
                            logger.LogDebug(
                                nameof(KeyVaultSecretsSoftDeletePurgeScheduler),
                                nameof(ScanAndPurgeAsync),
                                "Purged expired soft-deleted secret '{0}' from vault '{1}' in subscription '{2}'",
                                record.Secret.Name,
                                vault.Name,
                                subscription.SubscriptionId);
                        }
                    }
                }
            }

            ExecutedAt = DateTimeOffset.UtcNow;
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            logger.LogError(nameof(KeyVaultSecretsSoftDeletePurgeScheduler), nameof(ScanAndPurgeAsync), exception.Message);
            return Task.FromException(exception);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug(
            nameof(KeyVaultSecretsSoftDeletePurgeScheduler),
            nameof(StartAsync),
            "Soft-deleted secret purge scheduler started (interval: {0})",
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
