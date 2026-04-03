using Azure;
using Azure.Core;
using Azure.ResourceManager.Resources;
using Topaz.Portal.Models.ResourceGroups;
using Topaz.Portal.Models.ResourceManager;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<ListResourceGroupsResponse> ListResourceGroups()
    {
        await EnsureInitializedAsync();

        var subscriptions = await ListSubscriptions();
        var resourceGroups = new List<ResourceGroupDto>();

        foreach (var subscription in subscriptions.Value)
        {
            await foreach (var rg in _armClient!
                               .GetSubscriptionResource(
                                   new ResourceIdentifier($"/subscriptions/{subscription.SubscriptionId}"))
                               .GetResourceGroups().GetAllAsync())
            {
                resourceGroups.Add(new ResourceGroupDto
                {
                    Id = rg.Id.ToString(),
                    Name = rg.Data.Name,
                    Location = rg.Data.Location,
                    SubscriptionId = subscription.SubscriptionId,
                    SubscriptionName = subscription.DisplayName
                });
            }
        }

        return new ListResourceGroupsResponse
        {
            Value = resourceGroups.ToArray()
        };
    }

    public async Task<ListResourceGroupsResponse> ListResourceGroups(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetAsync(cancellationToken);

        var resourceGroups = new List<ResourceGroupDto>();
        await foreach (var rg in subscription.Value.GetResourceGroups().GetAllAsync(cancellationToken: cancellationToken))
        {
            resourceGroups.Add(new ResourceGroupDto
            {
                Id = rg.Id.ToString(),
                Name = rg.Data.Name,
                Location = rg.Data.Location,
                SubscriptionId = subscriptionId.ToString(),
                SubscriptionName = subscription.Value.Data.DisplayName
            });
        }

        return new ListResourceGroupsResponse { Value = resourceGroups.ToArray() };
    }

    public async Task<ResourceGroupDto?> GetResourceGroup(Guid subscriptionId, string resourceGroupName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        var subscription = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetAsync(cancellationToken);
        var rgResponse = await _armClient!
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetResourceGroupAsync(resourceGroupName, cancellationToken);

        var rg = rgResponse.Value;

        return new ResourceGroupDto
        {
            Id = rg.Id.ToString(),
            Name = rg.Data.Name,
            Location = rg.Data.Location,
            SubscriptionId = subscriptionId.ToString(),
            SubscriptionName = subscription.Value.Data.DisplayName,
            Tags = rg.Data.Tags is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(rg.Data.Tags, StringComparer.OrdinalIgnoreCase)
        };
    }

    public async Task CreateOrUpdateResourceGroupTag(
        Guid subscriptionId,
        string resourceGroupName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        if (string.IsNullOrWhiteSpace(tagValue))
            throw new ArgumentException("Tag value is required.", nameof(tagValue));

        var existing = await GetResourceGroup(subscriptionId, resourceGroupName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags[tagName] = tagValue;

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}", payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Updating resource group tags failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task DeleteResourceGroupTag(
        Guid subscriptionId,
        string resourceGroupName,
        string tagName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var existing = await GetResourceGroup(subscriptionId, resourceGroupName, cancellationToken);

        var tags = existing?.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Tags, StringComparer.OrdinalIgnoreCase);
        tags.Remove(tagName);

        var payload = new { Tags = tags };
        using var resp = await _httpClient.PatchAsJsonAsync(
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}", payload, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Deleting resource group tag failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. {body}");
        }
    }

    public async Task<ListDeploymentsResponse> ListDeployments(Guid subscriptionId, string resourceGroupName,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        var rg = await _armClient!.GetSubscriptionResource(
                new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetResourceGroupAsync(resourceGroupName, cancellationToken);
        var deployments = new List<DeploymentDto>();
        await foreach (var deployment in rg.Value.GetArmDeployments()
                           .GetAllAsync(cancellationToken: cancellationToken))
        {
            deployments.Add(new DeploymentDto
            {
                Id = deployment.Id.ToString(),
                Name = deployment.Data.Name,
                Type = deployment.Data.ResourceType,
                Location = deployment.Data.Location,
                Properties = new DeploymentPropertiesDto
                {
                    Mode = deployment.Data.Properties.Mode.HasValue
                        ? deployment.Data.Properties.Mode.Value.ToString()
                        : string.Empty,
                    ProvisioningState = deployment.Data.Properties.ProvisioningState.HasValue
                        ? deployment.Data.Properties.ProvisioningState.Value.ToString()
                        : string.Empty,
                    Timestamp = deployment.Data.Properties.Timestamp,
                }
            });
        }

        return new ListDeploymentsResponse()
        {
            Value = deployments.ToArray()
        };
    }

    public async Task<ArmDeploymentResource> GetDeployment(Guid subscriptionId, string resourceGroupName,
        string deploymentName, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(deploymentName))
            throw new ArgumentException("Deployment name is required.", nameof(deploymentName));

        var rg = await _armClient!.GetSubscriptionResource(
                new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetResourceGroupAsync(resourceGroupName, cancellationToken);

        var deployment = await rg.Value.GetArmDeploymentAsync(deploymentName, cancellationToken);
        return deployment.Value;
    }

    public async Task CreateResourceGroup(Guid subscriptionId, string resourceGroupName, string location,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("Subscription ID is required.", nameof(subscriptionId));

        if (string.IsNullOrWhiteSpace(resourceGroupName))
            throw new ArgumentException("Resource group name is required.", nameof(resourceGroupName));

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required.", nameof(location));

        var subscription = _armClient!.GetSubscriptionResource(
            new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        var rgCollection = subscription.GetResourceGroups();

        var rgData = new ResourceGroupData(new AzureLocation(location));

        _ = await rgCollection.CreateOrUpdateAsync(
            WaitUntil.Completed,
            resourceGroupName,
            rgData,
            cancellationToken);
    }
}
