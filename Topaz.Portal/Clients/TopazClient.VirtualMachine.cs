using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Topaz.Portal.Models.VirtualMachines;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<ListVirtualMachinesResponse> ListVirtualMachines(
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var subscriptions = await ListSubscriptions();
        var vms = new List<VirtualMachineDto>();

        foreach (var subscription in subscriptions.Value)
        {
            var url = $"/subscriptions/{subscription.SubscriptionId}/providers/Microsoft.Compute/virtualMachines?api-version=2024-07-01";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
                continue;

            var result = await response.Content.ReadFromJsonAsync<VmListResult>(cancellationToken: cancellationToken);

            if (result?.Value is null)
                continue;

            foreach (var vm in result.Value)
            {
                vms.Add(MapToVirtualMachineDto(vm, subscription.SubscriptionId, subscription.DisplayName));
            }
        }

        return new ListVirtualMachinesResponse { Value = vms.ToArray() };
    }

    public async Task<VirtualMachineDto?> GetVirtualMachine(
        Guid subscriptionId,
        string resourceGroupName,
        string vmName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(vmName))
            throw new ArgumentException("VM name is required.", nameof(vmName));

        var url = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Compute/virtualMachines/{vmName}?api-version=2024-07-01";
        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        var vm = await response.Content.ReadFromJsonAsync<VmItem>(cancellationToken: cancellationToken);

        if (vm is null)
            return null;

        var subscription = await GetSubscription(subscriptionId, cancellationToken);

        return MapToVirtualMachineDto(vm, subscriptionId.ToString(), subscription?.DisplayName);
    }

    public async Task CreateVirtualMachine(
        Guid subscriptionId,
        string resourceGroupName,
        string vmName,
        string location,
        string vmSize = "Standard_B2s",
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(vmName))
            throw new ArgumentException("VM name is required.", nameof(vmName));
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        var body = new
        {
            location,
            properties = new
            {
                hardwareProfile = new { vmSize }
            }
        };

        var url = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Compute/virtualMachines/{vmName}?api-version=2024-07-01";
        using var resp = await _httpClient.PutAsJsonAsync(url, body, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body2 = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Creating virtual machine failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body2}");
        }
    }

    public async Task DeleteVirtualMachine(
        Guid subscriptionId,
        string resourceGroupName,
        string vmName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(vmName))
            throw new ArgumentException("VM name is required.", nameof(vmName));

        var url = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Compute/virtualMachines/{vmName}?api-version=2024-07-01";
        using var resp = await _httpClient.DeleteAsync(url, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting virtual machine failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task CreateOrUpdateVirtualMachineTag(
        Guid subscriptionId,
        string resourceGroupName,
        string vmName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(vmName))
            throw new ArgumentException("VM name is required.", nameof(vmName));
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var existing = await GetVirtualMachine(subscriptionId, resourceGroupName, vmName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags[tagName] = tagValue;

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Compute/virtualMachines/{vmName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Updating virtual machine tags failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task DeleteVirtualMachineTag(
        Guid subscriptionId,
        string resourceGroupName,
        string vmName,
        string tagName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));
        if (string.IsNullOrWhiteSpace(vmName))
            throw new ArgumentException("VM name is required.", nameof(vmName));
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var existing = await GetVirtualMachine(subscriptionId, resourceGroupName, vmName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags.Remove(tagName);

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Compute/virtualMachines/{vmName}",
            payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting virtual machine tag failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    private static VirtualMachineDto MapToVirtualMachineDto(
        VmItem vm,
        string? subscriptionId,
        string? subscriptionName)
    {
        var rgName = ExtractResourceGroupFromId(vm.Id);

        return new VirtualMachineDto
        {
            Id = vm.Id,
            Name = vm.Name,
            Location = vm.Location,
            ResourceGroupName = rgName,
            SubscriptionId = subscriptionId,
            SubscriptionName = subscriptionName,
            VmSize = vm.Properties?.HardwareProfile?.VmSize,
            ProvisioningState = vm.Properties?.ProvisioningState,
            Tags = vm.Tags is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(vm.Tags, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static string? ExtractResourceGroupFromId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var parts = id.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (string.Equals(parts[i], "resourceGroups", StringComparison.OrdinalIgnoreCase))
                return parts[i + 1];
        }

        return null;
    }

    private sealed class VmListResult
    {
        [JsonPropertyName("value")]
        public VmItem[] Value { get; init; } = [];
    }

    private sealed class VmItem
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
        public VmProperties? Properties { get; init; }
    }

    private sealed class VmProperties
    {
        [JsonPropertyName("hardwareProfile")]
        public VmHardwareProfile? HardwareProfile { get; init; }

        [JsonPropertyName("provisioningState")]
        public string? ProvisioningState { get; init; }
    }

    private sealed class VmHardwareProfile
    {
        [JsonPropertyName("vmSize")]
        public string? VmSize { get; init; }
    }
}
