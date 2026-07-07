using Topaz.EventPipeline;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb;

internal sealed class ExpiredDocumentsPurgeScheduler : ITopazBackgroundService
{
    private readonly ICosmosDbDataPlane _dataPlane;
    private readonly ICosmosDbControlPlane _controlPlane;
    private readonly ISubscriptionLister _subscriptionControlPlane;
    private readonly ITopazLogger _logger;
    private readonly TimeSpan _interval;

    public ExpiredDocumentsPurgeScheduler(Pipeline eventPipeline, TimeSpan interval, ITopazLogger logger)
    {
        _dataPlane = new CosmosDbDataPlane(new DatabaseAccountResourceProvider(logger), logger);
        _controlPlane = CosmosDbServiceControlPlane.New(eventPipeline, logger);
        _subscriptionControlPlane = SubscriptionControlPlane.New(eventPipeline, logger);
        _logger = logger;
        _interval = interval;
    }

    internal ExpiredDocumentsPurgeScheduler(
        ICosmosDbDataPlane dataPlane,
        ICosmosDbControlPlane controlPlane,
        ISubscriptionLister subscriptionLister,
        ITopazLogger logger,
        TimeSpan interval)
    {
        _dataPlane = dataPlane;
        _controlPlane = controlPlane;
        _subscriptionControlPlane = subscriptionLister;
        _logger = logger;
        _interval = interval;
    }
    
    public string Name => $"Cosmos DB — expired documents purge (interval: {_interval})";

    public Task ScanAndUpdateAsync()
    {
        var subscriptions = _subscriptionControlPlane.List();
        if (subscriptions.Resource == null)
        {
            _logger.LogDebug(nameof(ExpiredDocumentsPurgeScheduler), nameof(ScanAndUpdateAsync), "No subscriptions found");
            return Task.CompletedTask;    
        }
        
        foreach (var subscription in subscriptions.Resource)
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(Guid.Parse(subscription.SubscriptionId));
            var accounts =
                _controlPlane.ListBySubscription(subscriptionIdentifier);

            if (accounts.Resource == null)
            {
                _logger.LogDebug(nameof(ExpiredDocumentsPurgeScheduler), nameof(ScanAndUpdateAsync), "No accounts found");
                continue;
            }

            foreach (var account in accounts.Resource)
            {
                var context = new CosmosDbAccountContext(account, subscriptionIdentifier, account.GetResourceGroupIdentifier());
                var databases = _dataPlane.ListDatabases(context);
                
                if (databases.Resource == null)
                {
                    _logger.LogDebug(nameof(ExpiredDocumentsPurgeScheduler), nameof(ScanAndUpdateAsync), "No databases found");
                    continue;
                }
                
                foreach(var database in databases.Resource)
                {
                    var containers = _dataPlane.ListCollections(context, database.Id);
                    
                    if (containers.Resource == null)
                    {
                        _logger.LogDebug(nameof(ExpiredDocumentsPurgeScheduler), nameof(ScanAndUpdateAsync), "No containers found");
                        continue;   
                    }

                    foreach (var container in containers.Resource)
                    {
                        var documents = _dataPlane.ListDocuments(context, database.Id, container.Id);
                        
                        if (documents.Resource == null)
                        {
                            _logger.LogDebug(nameof(ExpiredDocumentsPurgeScheduler), nameof(ScanAndUpdateAsync), "No documents found");
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
                            if (document["ttl"]?.GetValue<int?>() is not > 0) continue;
                            
                            var ttl = document["ttl"]!.GetValue<int>();
                            var ts = document["_ts"]?.GetValue<long>() ?? 0;
                            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > ts + ttl)
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
        _logger.LogDebug(
            nameof(ExpiredDocumentsPurgeScheduler),
            nameof(StartAsync),
            "Expired documents purge scheduler started (interval: {0})",
            _interval);

        using var timer = new PeriodicTimer(_interval);
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