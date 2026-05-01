using Azure;
using Azure.Core;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Topaz.Portal.Models.Storage;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<ListStorageAccountsResponse> ListStorageAccounts()
    {
        await EnsureInitializedAsync();

        var subscriptions = await ListSubscriptions();
        var storageAccounts = new List<StorageAccountDto>();

        foreach (var subscription in subscriptions.Value)
        {
            var subscriptionResource = _armClient!
                .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscription.SubscriptionId}"));

            await foreach (var sa in subscriptionResource.GetStorageAccountsAsync())
            {
                storageAccounts.Add(MapToStorageAccountDto(sa, subscription.SubscriptionId, subscription.DisplayName));
            }
        }

        return new ListStorageAccountsResponse { Value = storageAccounts.ToArray() };
    }

    public async Task<StorageAccountDto?> GetStorageAccount(
        Guid subscriptionId,
        string resourceGroupName,
        string storageAccountName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(storageAccountName))
            throw new ArgumentException("Storage account name is required.", nameof(storageAccountName));

        var resourceId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}");

        var sa = await _armClient!.GetStorageAccountResource(resourceId).GetAsync(cancellationToken: cancellationToken);

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetAsync(cancellationToken);

        return MapToStorageAccountDto(sa.Value, subscriptionId.ToString(), subscription.Value.Data.DisplayName, resourceGroupName);
    }

    public async Task CreateStorageAccount(
        Guid subscriptionId,
        string resourceGroupName,
        string storageAccountName,
        string location,
        string skuName = "Standard_LRS",
        string kind = "StorageV2",
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(storageAccountName))
            throw new ArgumentException("Storage account name is required.", nameof(storageAccountName));

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        var rg = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetResourceGroupAsync(resourceGroupName, cancellationToken);

        var content = new StorageAccountCreateOrUpdateContent(
            new StorageSku(new StorageSkuName(skuName)),
            new StorageKind(kind),
            new AzureLocation(location));

        await rg.Value.GetStorageAccounts().CreateOrUpdateAsync(
            WaitUntil.Completed,
            storageAccountName,
            content,
            cancellationToken);
    }

    public async Task DeleteStorageAccount(
        Guid subscriptionId,
        string resourceGroupName,
        string storageAccountName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(storageAccountName))
            throw new ArgumentException("Storage account name is required.", nameof(storageAccountName));

        var resourceId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}");

        await _armClient!.GetStorageAccountResource(resourceId).DeleteAsync(WaitUntil.Completed, cancellationToken);
    }

    public async Task CreateOrUpdateStorageAccountTag(
        Guid subscriptionId,
        string resourceGroupName,
        string storageAccountName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(storageAccountName))
            throw new ArgumentException("Storage account name is required.", nameof(storageAccountName));

        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        if (string.IsNullOrWhiteSpace(tagValue))
            throw new ArgumentException("Tag value is required.", nameof(tagValue));

        var existing = await GetStorageAccount(subscriptionId, resourceGroupName, storageAccountName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags[tagName] = tagValue;

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Updating storage account tags failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task DeleteStorageAccountTag(
        Guid subscriptionId,
        string resourceGroupName,
        string storageAccountName,
        string tagName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(storageAccountName))
            throw new ArgumentException("Storage account name is required.", nameof(storageAccountName));

        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var existing = await GetStorageAccount(subscriptionId, resourceGroupName, storageAccountName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags.Remove(tagName);

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{storageAccountName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting storage account tag failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    private static StorageAccountDto MapToStorageAccountDto(
        StorageAccountResource sa,
        string? subscriptionId,
        string? subscriptionName,
        string? resourceGroupName = null)
    {
        return new StorageAccountDto
        {
            Id = sa.Id?.ToString(),
            Name = sa.Data.Name,
            Location = sa.Data.Location,
            ResourceGroupName = resourceGroupName ?? sa.Id?.ResourceGroupName,
            SubscriptionId = subscriptionId,
            SubscriptionName = subscriptionName,
            Kind = sa.Data.Kind?.ToString(),
            SkuName = sa.Data.Sku?.Name.ToString(),
            BlobEndpoint = sa.Data.PrimaryEndpoints?.BlobUri?.ToString(),
            Tags = sa.Data.Tags is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(sa.Data.Tags, StringComparer.OrdinalIgnoreCase)
        };
    }
}
