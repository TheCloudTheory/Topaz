using Topaz.EventPipeline;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb;

internal sealed class ExpiredDocumentsPurgeScheduler(Pipeline eventPipeline, TimeSpan interval, ITopazLogger logger) : ITopazBackgroundService
{
    private readonly CosmosDbDataPlane _dataPlane = new(new DatabaseAccountResourceProvider(logger), logger);
    private readonly CosmosDbServiceControlPlane _controlPlane = CosmosDbServiceControlPlane.New(eventPipeline, logger);
    private readonly SubscriptionControlPlane _subscriptionControlPlane = SubscriptionControlPlane.New(eventPipeline, logger);
    
    public string Name => $"Cosmos DB — expired documents purge (interval: {interval})";

    public Task ScanAndUpdateAsync()
    {
        var subscriptions = _subscriptionControlPlane.List();
        if (subscriptions.Resource == null)
        {
            logger.LogDebug(nameof(ExpiredDocumentsPurgeScheduler), nameof(ScanAndUpdateAsync), "No subscriptions found");
            return Task.CompletedTask;    
        }
        
        foreach (var subscription in subscriptions.Resource)
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(Guid.Parse(subscription.SubscriptionId));
            var accounts =
                _controlPlane.ListBySubscription(subscriptionIdentifier);

            if (accounts.Resource == null)
            {
                logger.LogDebug(nameof(ExpiredDocumentsPurgeScheduler), nameof(ScanAndUpdateAsync), "No accounts found");
                continue;
            }

            foreach (var account in accounts.Resource)
            {
                var context = new CosmosDbAccountContext(account, subscriptionIdentifier, account.GetResourceGroupIdentifier());
                var databases = _dataPlane.ListDatabases(context);
                
                if (databases.Resource == null)
                {
                    logger.LogDebug(nameof(ExpiredDocumentsPurgeScheduler), nameof(ScanAndUpdateAsync), "No databases found");
                    continue;
                }
                
                foreach(var database in databases.Resource)
                {
                    var containers = _dataPlane.ListCollections(context, database.Id);
                    
                    if (containers.Resource == null)
                    {
                        logger.LogDebug(nameof(ExpiredDocumentsPurgeScheduler), nameof(ScanAndUpdateAsync), "No containers found");
                        continue;   
                    }

                    foreach (var container in containers.Resource)
                    {
                        var documents = _dataPlane.ListDocuments(context, database.Id, container.Id);
                        
                        if (documents.Resource == null)
                        {
                            logger.LogDebug(nameof(ExpiredDocumentsPurgeScheduler), nameof(ScanAndUpdateAsync), "No documents found");
                            continue;
                        }
                        
                        var defaultTtl = container.DefaultTtl;
                        
                        // Start with documents which has no TTL. If a container has a defaultTtl set,
                        // those documents need to be purged
                        var documentsWithoutTtl = documents.Resource.Where(d => d["ttl"] == null);
                        if(defaultTtl != null)
                        {
                            foreach (var document in documentsWithoutTtl)
                            {
                                _dataPlane.DeleteDocument(context, database.Id, container.Id, document["id"]?.ToString()!, string.Empty, null);
                            }
                        }
                        
                        // Now we can treat the documents with TTL
                        var documentsWithTtl = documents.Resource.Where(d => d["ttl"] != null);
                        foreach (var document in documentsWithTtl)
                        {
                            if (!document.ContainsKey("ttl") || !(document["ttl"]?.GetValue<int?>() > 0)) continue;
                            
                            var ttl = document["ttl"]?.GetValue<int>();
                            if (ttl <= 0)
                            {
                                _dataPlane.DeleteDocument(context, database.Id, container.Id, document["id"]?.ToString()!, string.Empty, null);
                            }

                        }
                    }
                }
            }
        }
        
        return Task.CompletedTask;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug(
            nameof(ExpiredDocumentsPurgeScheduler),
            nameof(StartAsync),
            "Expired documents purge scheduler started (interval: {0})",
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