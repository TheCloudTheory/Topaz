using System.Text;
using System.Text.Json;
using Topaz.Service.ResourceManager.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

internal sealed class ResourceManagerControlPlane(ResourceManagerResourceProvider provider)
{
    private readonly ArmTemplateParser _templateParser = new();

    public (OperationResult result, DeploymentResource resource) CreateOrUpdateDeployment(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string deploymentName, string content, string location, string deploymentMode)
    {
        var templateProperty =
            JsonDocument.Parse(content).RootElement.GetProperty("properties").GetProperty("template");
        var template = _templateParser.Parse(templateProperty.GetRawText());
        var deploymentResource = new DeploymentResource(subscriptionIdentifier, resourceGroupIdentifier, deploymentName,
            location, new DeploymentResourceProperties
            {
                CorrelationId = Guid.NewGuid().ToString(),
                Mode = deploymentMode,
                TemplateHash = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(template)))
            });

        provider.CreateOrUpdate(deploymentName, deploymentResource);
        
        return (OperationResult.Success, deploymentResource);
    }

    public (OperationResult result, DeploymentResource? resource) GetDeployment(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string deploymentName)
    {
        var resource = provider.GetAs<DeploymentResource>(deploymentName);
        if (resource == null ||
            !resource.IsInSubscription(subscriptionIdentifier) ||
            !resource.IsInResourceGroup(resourceGroupIdentifier))
        {
            return (OperationResult.NotFound, null);
        }

        return (OperationResult.Success, resource);
    }
    
    public OperationResult DeleteDeployment(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string deploymentName)
    {
        var resource = provider.GetAs<DeploymentResource>(deploymentName);
        if (resource == null ||
            !resource.IsInSubscription(subscriptionIdentifier) ||
            !resource.IsInResourceGroup(resourceGroupIdentifier))
        {
            return OperationResult.NotFound;
        }

        provider.Delete(deploymentName);
        return OperationResult.Deleted;
    }

    public (OperationResult result, DeploymentResource?[] resource) GetDeployments(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources = provider.ListAs<DeploymentResource>();
        if (resources == null)
        {
            return (OperationResult.Success, []);
        }

        var filteredBySubscriptionAndResourceGroup = resources.Where(deployment =>
            deployment != null &&
            deployment.IsInSubscription(subscriptionIdentifier) &&
            deployment.IsInResourceGroup(resourceGroupIdentifier));
        
        return (OperationResult.Success,  filteredBySubscriptionAndResourceGroup.ToArray());
    }
}