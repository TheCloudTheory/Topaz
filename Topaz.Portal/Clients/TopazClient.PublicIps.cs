using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Topaz.Portal.Models.PublicIps;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<ListPublicIpAddressesResponse> ListPublicIpAddresses(
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var subscriptions = await ListSubscriptions();
        var pips = new List<PublicIpAddressDto>();

        foreach (var subscription in subscriptions.Value)
        {
            var url = $"/subscriptions/{subscription.SubscriptionId}/providers/Microsoft.Network/publicIPAddresses?api-version=2024-05-01";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
                continue;

            var result = await response.Content.ReadFromJsonAsync<PipListResult>(cancellationToken: cancellationToken);

            if (result?.Value is null)
                continue;

            foreach (var pip in result.Value)
                pips.Add(MapToPublicIpAddressDto(pip, subscription.SubscriptionId, subscription.DisplayName));
        }

        return new ListPublicIpAddressesResponse { Value = pips.ToArray() };
    }

    public async Task<PublicIpAddressDto?> GetPublicIpAddress(
        Guid subscriptionId,
        string resourceGroupName,
        string pipName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(pipName))
            throw new ArgumentException("Public IP name is required.", nameof(pipName));

        var url = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/publicIPAddresses/{pipName}?api-version=2024-05-01";
        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        var pip = await response.Content.ReadFromJsonAsync<PipItem>(cancellationToken: cancellationToken);

        if (pip is null)
            return null;

        var subscription = await GetSubscription(subscriptionId, cancellationToken);

        return MapToPublicIpAddressDto(pip, subscriptionId.ToString(), subscription?.DisplayName);
    }

    public async Task CreatePublicIpAddress(
        Guid subscriptionId,
        string resourceGroupName,
        string pipName,
        string location,
        string allocationMethod = "Static",
        string ipVersion = "IPv4",
        string sku = "Standard",
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(pipName))
            throw new ArgumentException("Public IP name is required.", nameof(pipName));
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        var body = new
        {
            location,
            sku = new { name = sku },
            properties = new
            {
                publicIPAllocationMethod = allocationMethod,
                publicIPAddressVersion = ipVersion
            }
        };

        var url = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/publicIPAddresses/{pipName}?api-version=2024-05-01";
        using var resp = await _httpClient.PutAsJsonAsync(url, body, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var respBody = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Creating public IP address failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {respBody}");
        }
    }

    public async Task DeletePublicIpAddress(
        Guid subscriptionId,
        string resourceGroupName,
        string pipName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(pipName))
            throw new ArgumentException("Public IP name is required.", nameof(pipName));

        var url = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/publicIPAddresses/{pipName}?api-version=2024-05-01";
        using var resp = await _httpClient.DeleteAsync(url, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting public IP address failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task CreateOrUpdatePublicIpAddressTag(
        Guid subscriptionId,
        string resourceGroupName,
        string pipName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(pipName))
            throw new ArgumentException("Public IP name is required.", nameof(pipName));
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var existing = await GetPublicIpAddress(subscriptionId, resourceGroupName, pipName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags[tagName] = tagValue;

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/publicIPAddresses/{pipName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Updating public IP address tags failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task DeletePublicIpAddressTag(
        Guid subscriptionId,
        string resourceGroupName,
        string pipName,
        string tagName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(pipName))
            throw new ArgumentException("Public IP name is required.", nameof(pipName));
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var existing = await GetPublicIpAddress(subscriptionId, resourceGroupName, pipName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags.Remove(tagName);

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/publicIPAddresses/{pipName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting public IP address tag failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    private static PublicIpAddressDto MapToPublicIpAddressDto(
        PipItem pip,
        string? subscriptionId,
        string? subscriptionName)
    {
        var rgName = ExtractResourceGroupFromId(pip.Id);

        return new PublicIpAddressDto
        {
            Id = pip.Id,
            Name = pip.Name,
            Location = pip.Location,
            ResourceGroupName = rgName,
            SubscriptionId = subscriptionId,
            SubscriptionName = subscriptionName,
            IpAddress = pip.Properties?.IpAddress,
            AllocationMethod = pip.Properties?.PublicIPAllocationMethod,
            IpVersion = pip.Properties?.PublicIPAddressVersion,
            Sku = pip.Sku?.Name,
            IdleTimeoutInMinutes = pip.Properties?.IdleTimeoutInMinutes,
            ProvisioningState = pip.Properties?.ProvisioningState,
            Tags = pip.Tags is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(pip.Tags, StringComparer.OrdinalIgnoreCase)
        };
    }

    private sealed class PipListResult
    {
        [JsonPropertyName("value")]
        public PipItem[] Value { get; init; } = [];
    }

    private sealed class PipItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("location")]
        public string? Location { get; init; }

        [JsonPropertyName("sku")]
        public PipSku? Sku { get; init; }

        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; init; }

        [JsonPropertyName("properties")]
        public PipItemProperties? Properties { get; init; }
    }

    private sealed class PipSku
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    private sealed class PipItemProperties
    {
        [JsonPropertyName("publicIPAllocationMethod")]
        public string? PublicIPAllocationMethod { get; init; }

        [JsonPropertyName("publicIPAddressVersion")]
        public string? PublicIPAddressVersion { get; init; }

        [JsonPropertyName("ipAddress")]
        public string? IpAddress { get; init; }

        [JsonPropertyName("idleTimeoutInMinutes")]
        public int? IdleTimeoutInMinutes { get; init; }

        [JsonPropertyName("provisioningState")]
        public string? ProvisioningState { get; init; }
    }
}
