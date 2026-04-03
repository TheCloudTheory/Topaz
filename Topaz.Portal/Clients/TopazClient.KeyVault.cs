using Azure;
using Azure.Core;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Topaz.Portal.Models.KeyVaults;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<ListKeyVaultsResponse> ListKeyVaults()
    {
        await EnsureInitializedAsync();

        var subscriptions = await ListSubscriptions();
        var keyVaults = new List<KeyVaultDto>();

        foreach (var subscription in subscriptions.Value)
        {
            var subscriptionResource = _armClient!
                .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscription.SubscriptionId}"));

            await foreach (var kv in subscriptionResource.GetKeyVaultsAsync())
            {
                keyVaults.Add(new KeyVaultDto
                {
                    Id = kv.Id.ToString(),
                    Name = kv.Data.Name,
                    Location = kv.Data.Location,
                    ResourceGroupName = kv.Id.ResourceGroupName,
                    SubscriptionId = subscription.SubscriptionId,
                    SubscriptionName = subscription.DisplayName,
                    VaultUri = kv.Data.Properties?.VaultUri?.ToString(),
                    SkuName = kv.Data.Properties?.Sku?.Name.ToString()
                });
            }
        }

        return new ListKeyVaultsResponse
        {
            Value = keyVaults.ToArray()
        };
    }

    public async Task CreateKeyVault(
        Guid subscriptionId,
        string resourceGroupName,
        string keyVaultName,
        string location,
        string skuName = "Standard",
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(keyVaultName))
            throw new ArgumentException("Key vault name is required.", nameof(keyVaultName));

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        var rg = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetResourceGroupAsync(resourceGroupName, cancellationToken);

        var skuNameValue = skuName.Equals("Premium", StringComparison.OrdinalIgnoreCase)
            ? KeyVaultSkuName.Premium
            : KeyVaultSkuName.Standard;

        var content = new KeyVaultCreateOrUpdateContent(
            new AzureLocation(location),
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, skuNameValue)));

        _ = await rg.Value.GetKeyVaults().CreateOrUpdateAsync(
            WaitUntil.Completed,
            keyVaultName,
            content,
            cancellationToken);
    }

    public async Task<KeyVaultDto?> GetKeyVault(
        Guid subscriptionId,
        string resourceGroupName,
        string keyVaultName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(keyVaultName))
            throw new ArgumentException("Key vault name is required.", nameof(keyVaultName));

        var vaultId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}");

        var vault = await _armClient!.GetKeyVaultResource(vaultId).GetAsync(cancellationToken);

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetAsync(cancellationToken);

        return new KeyVaultDto
        {
            Id = vault.Value.Id.ToString(),
            Name = vault.Value.Data.Name,
            Location = vault.Value.Data.Location,
            ResourceGroupName = resourceGroupName,
            SubscriptionId = subscriptionId.ToString(),
            SubscriptionName = subscription.Value.Data.DisplayName,
            VaultUri = vault.Value.Data.Properties?.VaultUri?.ToString(),
            SkuName = vault.Value.Data.Properties?.Sku?.Name.ToString(),
            Tags = vault.Value.Data.Tags is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(vault.Value.Data.Tags, StringComparer.OrdinalIgnoreCase)
        };
    }

    public async Task CreateOrUpdateKeyVaultTag(
        Guid subscriptionId,
        string resourceGroupName,
        string keyVaultName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(keyVaultName))
            throw new ArgumentException("Key vault name is required.", nameof(keyVaultName));

        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        if (string.IsNullOrWhiteSpace(tagValue))
            throw new ArgumentException("Tag value is required.", nameof(tagValue));

        var existing = await GetKeyVault(subscriptionId, resourceGroupName, keyVaultName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags[tagName] = tagValue;

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Updating key vault tags failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task DeleteKeyVaultTag(
        Guid subscriptionId,
        string resourceGroupName,
        string keyVaultName,
        string tagName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(keyVaultName))
            throw new ArgumentException("Key vault name is required.", nameof(keyVaultName));

        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var existing = await GetKeyVault(subscriptionId, resourceGroupName, keyVaultName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags.Remove(tagName);

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting key vault tag failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }
}
