using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration;

internal sealed class AppConfigurationSoftDeletePurgeScheduler(
    SubscriptionControlPlane subscriptionControlPlane,
    AppConfigurationServiceControlPlane controlPlane,
    TimeSpan interval,
    ITopazLogger logger) : ITopazBackgroundService
{
    public string Name => $"App Configuration — soft-deleted secret purge (interval: {interval})";
    public DateTimeOffset? ExecutedAt { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug(
            nameof(AppConfigurationSoftDeletePurgeScheduler),
            nameof(StartAsync),
            "Soft-deleted App Configuration purge scheduler started (interval: {0})",
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
                var deletedOperation = controlPlane.ListDeleted(subscriptionIdentifier);

                if (deletedOperation.Resource == null) continue;

                foreach (var deletedStore in deletedOperation.Resource)
                {
                    if (deletedStore.ScheduledPurgeDate == null || deletedStore.ScheduledPurgeDate > DateTimeOffset.UtcNow)
                        continue;

                    var purgeOperation = controlPlane.Purge(subscriptionIdentifier, deletedStore.Name);
                    if (purgeOperation.Result == OperationResult.Success)
                    {
                        logger.LogDebug(
                            nameof(AppConfigurationSoftDeletePurgeScheduler),
                            nameof(ScanAndPurgeAsync),
                            "Purged expired soft-deleted AppConfiguration '{0}' in subscription '{1}'",
                            deletedStore.Name,
                            subscription.SubscriptionId);
                    }
                }
            }

            ExecutedAt = DateTimeOffset.UtcNow;
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            logger.LogError(nameof(AppConfigurationSoftDeletePurgeScheduler), nameof(ScanAndPurgeAsync),
                exception.Message);
            return Task.FromException(exception);
        }
    }
}