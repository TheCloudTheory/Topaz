using Azure;
using Azure.Core;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.EventHubs.Models;
using Topaz.Portal.Models.EventHubs;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<ListEventHubNamespacesResponse> ListEventHubNamespaces(
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var subscriptions = await ListSubscriptions();
        var namespaces = new List<EventHubNamespaceDto>();

        foreach (var subscription in subscriptions.Value)
        {
            var subscriptionResource = _armClient!
                .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscription.SubscriptionId}"));

            await foreach (var ns in subscriptionResource.GetEventHubsNamespacesAsync(cancellationToken: cancellationToken))
            {
                namespaces.Add(MapToNamespaceDto(ns, subscription.SubscriptionId, subscription.DisplayName));
            }
        }

        return new ListEventHubNamespacesResponse { Value = namespaces.ToArray() };
    }

    public async Task<EventHubNamespaceDto?> GetEventHubNamespace(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var resourceId = EventHubsNamespaceResource.CreateResourceIdentifier(
            subscriptionId.ToString(), resourceGroupName, namespaceName);

        var ns = await _armClient!.GetEventHubsNamespaceResource(resourceId).GetAsync(cancellationToken: cancellationToken);

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetAsync(cancellationToken);

        return MapToNamespaceDto(ns.Value, subscriptionId.ToString(), subscription.Value.Data.DisplayName, resourceGroupName);
    }

    public async Task CreateEventHubNamespace(
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

        var data = new EventHubsNamespaceData(new AzureLocation(location))
        {
            Sku = new EventHubsSku(EventHubsSkuName.Standard)
        };

        await rg.Value.GetEventHubsNamespaces().CreateOrUpdateAsync(
            WaitUntil.Completed, namespaceName, data, cancellationToken);
    }

    public async Task DeleteEventHubNamespace(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var resourceId = EventHubsNamespaceResource.CreateResourceIdentifier(
            subscriptionId.ToString(), resourceGroupName, namespaceName);

        await _armClient!.GetEventHubsNamespaceResource(resourceId).DeleteAsync(WaitUntil.Completed, cancellationToken);
    }

    public async Task CreateOrUpdateEventHubNamespaceTag(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var existing = await GetEventHubNamespace(subscriptionId, resourceGroupName, namespaceName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags[tagName] = tagValue;

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventHub/namespaces/{namespaceName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Updating namespace tags failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task DeleteEventHubNamespaceTag(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string tagName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var existing = await GetEventHubNamespace(subscriptionId, resourceGroupName, namespaceName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags.Remove(tagName);

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventHub/namespaces/{namespaceName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting namespace tag failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task<ListEventHubsResponse> ListEventHubs(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var resourceId = EventHubsNamespaceResource.CreateResourceIdentifier(
            subscriptionId.ToString(), resourceGroupName, namespaceName);

        var ns = _armClient!.GetEventHubsNamespaceResource(resourceId);
        var hubs = new List<EventHubDto>();

        await foreach (var hub in ns.GetEventHubs().GetAllAsync(cancellationToken: cancellationToken))
        {
            hubs.Add(MapToEventHubDto(hub, namespaceName));
        }

        return new ListEventHubsResponse { Value = hubs.ToArray() };
    }

    public async Task CreateEventHub(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string eventHubName,
        int partitionCount = 4,
        int messageRetentionInDays = 1,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var resourceId = EventHubsNamespaceResource.CreateResourceIdentifier(
            subscriptionId.ToString(), resourceGroupName, namespaceName);

        var ns = _armClient!.GetEventHubsNamespaceResource(resourceId);
        var data = new EventHubData
        {
            PartitionCount = partitionCount,
#pragma warning disable CS0618
            MessageRetentionInDays = messageRetentionInDays
#pragma warning restore CS0618
        };

        await ns.GetEventHubs().CreateOrUpdateAsync(WaitUntil.Completed, eventHubName, data, cancellationToken);
    }

    public async Task DeleteEventHub(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string eventHubName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var resourceId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventHub/namespaces/{namespaceName}/eventhubs/{eventHubName}");

        await _armClient!.GetEventHubResource(resourceId).DeleteAsync(WaitUntil.Completed, cancellationToken);
    }

    private static EventHubNamespaceDto MapToNamespaceDto(
        EventHubsNamespaceResource ns,
        string? subscriptionId,
        string? subscriptionName,
        string? resourceGroupName = null)
    {
        return new EventHubNamespaceDto
        {
            Id = ns.Id?.ToString(),
            Name = ns.Data.Name,
            Location = ns.Data.Location.ToString(),
            ResourceGroupName = resourceGroupName ?? ns.Id?.ResourceGroupName,
            SubscriptionId = subscriptionId,
            SubscriptionName = subscriptionName,
            ProvisioningState = ns.Data.ProvisioningState,
            ServiceBusEndpoint = ns.Data.ServiceBusEndpoint,
            Status = ns.Data.Status,
            KafkaEnabled = ns.Data.KafkaEnabled,
            ZoneRedundant = ns.Data.ZoneRedundant,
            IsAutoInflateEnabled = ns.Data.IsAutoInflateEnabled,
            MaximumThroughputUnits = ns.Data.MaximumThroughputUnits,
            MinimumTlsVersion = ns.Data.MinimumTlsVersion?.ToString(),
            SkuName = ns.Data.Sku?.Name.ToString(),
            SkuTier = ns.Data.Sku?.Tier?.ToString(),
            SkuCapacity = ns.Data.Sku?.Capacity,
            Tags = ns.Data.Tags is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(ns.Data.Tags, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static EventHubDto MapToEventHubDto(EventHubResource hub, string namespaceName)
    {
#pragma warning disable CS0618
        return new EventHubDto
        {
            Id = hub.Id?.ToString(),
            Name = hub.Data.Name,
            NamespaceName = namespaceName,
            Status = hub.Data.Status?.ToString(),
            PartitionCount = (int)(hub.Data.PartitionCount ?? 0),
            MessageRetentionInDays = (int)(hub.Data.MessageRetentionInDays ?? 0),
            PartitionIds = hub.Data.PartitionIds ?? [],
            CreatedOn = hub.Data.CreatedOn,
            UpdatedOn = hub.Data.UpdatedOn
        };
#pragma warning restore CS0618
    }
}
