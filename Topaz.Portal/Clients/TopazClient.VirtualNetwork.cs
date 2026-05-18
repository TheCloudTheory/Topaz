using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Topaz.Portal.Models.VirtualNetworks;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<ListVirtualNetworksResponse> ListVirtualNetworks(
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var subscriptions = await ListSubscriptions();
        var vnets = new List<VirtualNetworkDto>();

        foreach (var subscription in subscriptions.Value)
        {
            var url = $"/subscriptions/{subscription.SubscriptionId}/providers/Microsoft.Network/virtualNetworks?api-version=2024-05-01";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
                continue;

            var result = await response.Content.ReadFromJsonAsync<VnetListResult>(cancellationToken: cancellationToken);

            if (result?.Value is null)
                continue;

            foreach (var vnet in result.Value)
            {
                vnets.Add(MapToVirtualNetworkDto(vnet, subscription.SubscriptionId, subscription.DisplayName));
            }
        }

        return new ListVirtualNetworksResponse { Value = vnets.ToArray() };
    }

    public async Task<VirtualNetworkDto?> GetVirtualNetwork(
        Guid subscriptionId,
        string resourceGroupName,
        string vnetName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(vnetName))
            throw new ArgumentException("VNet name is required.", nameof(vnetName));

        var url = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks/{vnetName}?api-version=2024-05-01";
        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        var vnet = await response.Content.ReadFromJsonAsync<VnetItem>(cancellationToken: cancellationToken);

        if (vnet is null)
            return null;

        var subscription = await GetSubscription(subscriptionId, cancellationToken);

        return MapToVirtualNetworkDto(vnet, subscriptionId.ToString(), subscription?.DisplayName);
    }

    public async Task CreateVirtualNetwork(
        Guid subscriptionId,
        string resourceGroupName,
        string vnetName,
        string location,
        string addressPrefix = "10.0.0.0/16",
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(vnetName))
            throw new ArgumentException("VNet name is required.", nameof(vnetName));
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        var body = new
        {
            location,
            properties = new
            {
                addressSpace = new
                {
                    addressPrefixes = new[] { addressPrefix }
                }
            }
        };

        var url = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks/{vnetName}?api-version=2024-05-01";
        using var resp = await _httpClient.PutAsJsonAsync(url, body, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body2 = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Creating virtual network failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body2}");
        }
    }

    public async Task DeleteVirtualNetwork(
        Guid subscriptionId,
        string resourceGroupName,
        string vnetName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(vnetName))
            throw new ArgumentException("VNet name is required.", nameof(vnetName));

        var url = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks/{vnetName}?api-version=2024-05-01";
        using var resp = await _httpClient.DeleteAsync(url, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting virtual network failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task CreateOrUpdateVirtualNetworkTag(
        Guid subscriptionId,
        string resourceGroupName,
        string vnetName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(vnetName))
            throw new ArgumentException("VNet name is required.", nameof(vnetName));
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var existing = await GetVirtualNetwork(subscriptionId, resourceGroupName, vnetName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags[tagName] = tagValue;

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks/{vnetName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Updating virtual network tags failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task DeleteVirtualNetworkTag(
        Guid subscriptionId,
        string resourceGroupName,
        string vnetName,
        string tagName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(vnetName))
            throw new ArgumentException("VNet name is required.", nameof(vnetName));
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var existing = await GetVirtualNetwork(subscriptionId, resourceGroupName, vnetName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags.Remove(tagName);

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks/{vnetName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting virtual network tag failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    private static VirtualNetworkDto MapToVirtualNetworkDto(
        VnetItem vnet,
        string? subscriptionId,
        string? subscriptionName)
    {
        var rgName = ExtractResourceGroupFromId(vnet.Id);

        return new VirtualNetworkDto
        {
            Id = vnet.Id,
            Name = vnet.Name,
            Location = vnet.Location,
            ResourceGroupName = rgName,
            SubscriptionId = subscriptionId,
            SubscriptionName = subscriptionName,
            AddressPrefixes = vnet.Properties?.AddressSpace?.AddressPrefixes,
            ProvisioningState = vnet.Properties?.ProvisioningState,
            Tags = vnet.Tags is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(vnet.Tags, StringComparer.OrdinalIgnoreCase)
        };
    }

    private sealed class VnetListResult
    {
        [JsonPropertyName("value")]
        public VnetItem[] Value { get; init; } = [];
    }

    private sealed class VnetItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("location")]
        public string? Location { get; init; }

        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; init; }

        [JsonPropertyName("properties")]
        public VnetProperties? Properties { get; init; }
    }

    private sealed class VnetProperties
    {
        [JsonPropertyName("addressSpace")]
        public VnetAddressSpace? AddressSpace { get; init; }

        [JsonPropertyName("provisioningState")]
        public string? ProvisioningState { get; init; }
    }

    private sealed class VnetAddressSpace
    {
        [JsonPropertyName("addressPrefixes")]
        public List<string>? AddressPrefixes { get; init; }
    }
}
