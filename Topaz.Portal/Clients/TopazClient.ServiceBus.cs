using Azure;
using Azure.Core;
using Azure.ResourceManager.ServiceBus;
using Azure.ResourceManager.ServiceBus.Models;
using Topaz.Portal.Models.ServiceBus;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<ListServiceBusNamespacesResponse> ListServiceBusNamespaces(
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var subscriptions = await ListSubscriptions();
        var namespaces = new List<ServiceBusNamespaceDto>();

        foreach (var subscription in subscriptions.Value)
        {
            var subscriptionResource = _armClient!
                .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscription.SubscriptionId}"));

            await foreach (var ns in subscriptionResource.GetServiceBusNamespacesAsync(cancellationToken: cancellationToken))
            {
                namespaces.Add(MapToServiceBusNamespaceDto(ns, subscription.SubscriptionId, subscription.DisplayName));
            }
        }

        return new ListServiceBusNamespacesResponse { Value = namespaces.ToArray() };
    }

    public async Task<ServiceBusNamespaceDto?> GetServiceBusNamespace(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var resourceId = ServiceBusNamespaceResource.CreateResourceIdentifier(
            subscriptionId.ToString(), resourceGroupName, namespaceName);

        var ns = await _armClient!.GetServiceBusNamespaceResource(resourceId).GetAsync(cancellationToken: cancellationToken);

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetAsync(cancellationToken);

        return MapToServiceBusNamespaceDto(ns.Value, subscriptionId.ToString(), subscription.Value.Data.DisplayName, resourceGroupName);
    }

    public async Task CreateServiceBusNamespace(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string location,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var rg = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetResourceGroupAsync(resourceGroupName, cancellationToken);

        var data = new ServiceBusNamespaceData(new AzureLocation(location))
        {
            Sku = new ServiceBusSku(ServiceBusSkuName.Standard)
        };

        await rg.Value.GetServiceBusNamespaces().CreateOrUpdateAsync(
            WaitUntil.Completed, namespaceName, data, cancellationToken);
    }

    public async Task DeleteServiceBusNamespace(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var resourceId = ServiceBusNamespaceResource.CreateResourceIdentifier(
            subscriptionId.ToString(), resourceGroupName, namespaceName);

        await _armClient!.GetServiceBusNamespaceResource(resourceId).DeleteAsync(WaitUntil.Completed, cancellationToken);
    }

    public async Task CreateOrUpdateServiceBusNamespaceTag(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var existing = await GetServiceBusNamespace(subscriptionId, resourceGroupName, namespaceName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags[tagName] = tagValue;

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Updating namespace tags failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task DeleteServiceBusNamespaceTag(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string tagName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var existing = await GetServiceBusNamespace(subscriptionId, resourceGroupName, namespaceName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags.Remove(tagName);

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting namespace tag failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task<ListServiceBusQueuesResponse> ListServiceBusQueues(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var resourceId = ServiceBusNamespaceResource.CreateResourceIdentifier(
            subscriptionId.ToString(), resourceGroupName, namespaceName);

        var ns = _armClient!.GetServiceBusNamespaceResource(resourceId);
        var queues = new List<ServiceBusQueueDto>();

        await foreach (var queue in ns.GetServiceBusQueues().GetAllAsync(cancellationToken: cancellationToken))
        {
            queues.Add(new ServiceBusQueueDto
            {
                Id = queue.Id?.ToString(),
                Name = queue.Data.Name,
                NamespaceName = namespaceName,
                Status = queue.Data.Status?.ToString(),
                MessageCount = queue.Data.MessageCount,
                SizeInBytes = queue.Data.SizeInBytes,
                MaxDeliveryCount = queue.Data.MaxDeliveryCount,
                MaxSizeInMegabytes = queue.Data.MaxSizeInMegabytes,
                RequiresSession = queue.Data.RequiresSession,
                RequiresDuplicateDetection = queue.Data.RequiresDuplicateDetection,
                CreatedOn = queue.Data.CreatedOn,
                UpdatedOn = queue.Data.UpdatedOn
            });
        }

        return new ListServiceBusQueuesResponse { Value = queues.ToArray() };
    }

    public async Task CreateServiceBusQueue(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string queueName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var resourceId = ServiceBusNamespaceResource.CreateResourceIdentifier(
            subscriptionId.ToString(), resourceGroupName, namespaceName);

        var ns = _armClient!.GetServiceBusNamespaceResource(resourceId);
        var data = new ServiceBusQueueData();

        await ns.GetServiceBusQueues().CreateOrUpdateAsync(WaitUntil.Completed, queueName, data, cancellationToken);
    }

    public async Task DeleteServiceBusQueue(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string queueName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var resourceId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/queues/{queueName}");

        await _armClient!.GetServiceBusQueueResource(resourceId).DeleteAsync(WaitUntil.Completed, cancellationToken);
    }

    public async Task<ListServiceBusTopicsResponse> ListServiceBusTopics(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var resourceId = ServiceBusNamespaceResource.CreateResourceIdentifier(
            subscriptionId.ToString(), resourceGroupName, namespaceName);

        var ns = _armClient!.GetServiceBusNamespaceResource(resourceId);
        var topics = new List<ServiceBusTopicDto>();

        await foreach (var topic in ns.GetServiceBusTopics().GetAllAsync(cancellationToken: cancellationToken))
        {
            topics.Add(new ServiceBusTopicDto
            {
                Id = topic.Id?.ToString(),
                Name = topic.Data.Name,
                NamespaceName = namespaceName,
                Status = topic.Data.Status?.ToString(),
                SizeInBytes = topic.Data.SizeInBytes,
                SubscriptionCount = topic.Data.SubscriptionCount,
                MaxSizeInMegabytes = topic.Data.MaxSizeInMegabytes,
                RequiresDuplicateDetection = topic.Data.RequiresDuplicateDetection,
                CreatedOn = topic.Data.CreatedOn,
                UpdatedOn = topic.Data.UpdatedOn
            });
        }

        return new ListServiceBusTopicsResponse { Value = topics.ToArray() };
    }

    public async Task CreateServiceBusTopic(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string topicName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var resourceId = ServiceBusNamespaceResource.CreateResourceIdentifier(
            subscriptionId.ToString(), resourceGroupName, namespaceName);

        var ns = _armClient!.GetServiceBusNamespaceResource(resourceId);
        var data = new ServiceBusTopicData();

        await ns.GetServiceBusTopics().CreateOrUpdateAsync(WaitUntil.Completed, topicName, data, cancellationToken);
    }

    public async Task DeleteServiceBusTopic(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string topicName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var resourceId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/topics/{topicName}");

        await _armClient!.GetServiceBusTopicResource(resourceId).DeleteAsync(WaitUntil.Completed, cancellationToken);
    }

    private static ServiceBusNamespaceDto MapToServiceBusNamespaceDto(
        ServiceBusNamespaceResource ns,
        string? subscriptionId,
        string? subscriptionName,
        string? resourceGroupName = null)
    {
        return new ServiceBusNamespaceDto
        {
            Id = ns.Id?.ToString(),
            Name = ns.Data.Name,
            Location = ns.Data.Location.ToString(),
            ResourceGroupName = resourceGroupName ?? ns.Id?.ResourceGroupName,
            SubscriptionId = subscriptionId,
            SubscriptionName = subscriptionName,
            ProvisioningState = ns.Data.ProvisioningState,
            Status = ns.Data.Status?.ToString(),
            ServiceBusEndpoint = ns.Data.ServiceBusEndpoint,
            MetricId = ns.Data.MetricId,
            SkuName = ns.Data.Sku?.Name.ToString(),
            SkuTier = ns.Data.Sku?.Tier?.ToString(),
            SkuCapacity = ns.Data.Sku?.Capacity,
            IsZoneRedundant = ns.Data.IsZoneRedundant,
            MinimumTlsVersion = ns.Data.MinimumTlsVersion?.ToString(),
            DisableLocalAuth = ns.Data.DisableLocalAuth,
            PremiumMessagingPartitions = ns.Data.PremiumMessagingPartitions,
            CreatedOn = ns.Data.CreatedOn,
            UpdatedOn = ns.Data.UpdatedOn,
            Tags = ns.Data.Tags is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(ns.Data.Tags, StringComparer.OrdinalIgnoreCase)
        };
    }
}
