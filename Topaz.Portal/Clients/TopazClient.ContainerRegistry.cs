using Azure;
using Azure.Core;
using Azure.ResourceManager.ContainerRegistry;
using Azure.ResourceManager.ContainerRegistry.Models;
using Topaz.Portal.Models.ContainerRegistry;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<ListContainerRegistriesResponse> ListContainerRegistries()
    {
        await EnsureInitializedAsync();

        var subscriptions = await ListSubscriptions();
        var registries = new List<ContainerRegistryDto>();

        foreach (var subscription in subscriptions.Value)
        {
            var subscriptionResource = _armClient!
                .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscription.SubscriptionId}"));

            await foreach (var registry in subscriptionResource.GetContainerRegistriesAsync())
            {
                registries.Add(new ContainerRegistryDto
                {
                    Id = registry.Id.ToString(),
                    Name = registry.Data.Name,
                    Location = registry.Data.Location,
                    ResourceGroupName = registry.Id.ResourceGroupName,
                    SubscriptionId = subscription.SubscriptionId,
                    SubscriptionName = subscription.DisplayName,
                    LoginServer = registry.Data.LoginServer,
                    SkuName = registry.Data.Sku?.Name.ToString(),
                    AdminUserEnabled = registry.Data.IsAdminUserEnabled.GetValueOrDefault(false)
                });
            }
        }

        return new ListContainerRegistriesResponse
        {
            Value = registries.ToArray()
        };
    }

    public async Task CreateContainerRegistry(
        Guid subscriptionId,
        string resourceGroupName,
        string registryName,
        string location,
        string skuName = "Basic",
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(registryName))
            throw new ArgumentException("Registry name is required.", nameof(registryName));

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        var rg = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetResourceGroupAsync(resourceGroupName, cancellationToken);

        var sku = skuName switch
        {
            "Premium" => ContainerRegistrySkuName.Premium,
            "Standard" => ContainerRegistrySkuName.Standard,
            _ => ContainerRegistrySkuName.Basic
        };

        var content = new ContainerRegistryData(
            new AzureLocation(location),
            new ContainerRegistrySku(sku));

        _ = await rg.Value.GetContainerRegistries().CreateOrUpdateAsync(
            WaitUntil.Completed,
            registryName,
            content,
            cancellationToken);
    }

    public async Task DeleteContainerRegistry(
        Guid subscriptionId,
        string resourceGroupName,
        string registryName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(registryName))
            throw new ArgumentException("Registry name is required.", nameof(registryName));

        var resourceId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}");

        _ = await _armClient!.GetContainerRegistryResource(resourceId).DeleteAsync(
            WaitUntil.Completed,
            cancellationToken);
    }

    public async Task<ContainerRegistryDto?> GetContainerRegistry(
        Guid subscriptionId,
        string resourceGroupName,
        string registryName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(registryName))
            throw new ArgumentException("Registry name is required.", nameof(registryName));

        var resourceId = new ResourceIdentifier(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}");

        var registry = await _armClient!.GetContainerRegistryResource(resourceId).GetAsync(cancellationToken);

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetAsync(cancellationToken);

        return new ContainerRegistryDto
        {
            Id = registry.Value.Id.ToString(),
            Name = registry.Value.Data.Name,
            Location = registry.Value.Data.Location,
            ResourceGroupName = resourceGroupName,
            SubscriptionId = subscriptionId.ToString(),
            SubscriptionName = subscription.Value.Data.DisplayName,
            LoginServer = registry.Value.Data.LoginServer,
            SkuName = registry.Value.Data.Sku?.Name.ToString(),
            AdminUserEnabled = registry.Value.Data.IsAdminUserEnabled.GetValueOrDefault(false),
            Tags = registry.Value.Data.Tags is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(registry.Value.Data.Tags, StringComparer.OrdinalIgnoreCase)
        };
    }
}
