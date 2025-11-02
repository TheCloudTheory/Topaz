using Azure.Deployments.Core.Definitions.Schema;
using Azure.Deployments.Core.Entities;
using Azure.Deployments.Templates.Extensions;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Topaz.Service.ResourceGroup.Models;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.ResourceManager.Models;
using Topaz.Service.ResourceManager.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using DeploymentResource = Topaz.Service.ResourceManager.Models.DeploymentResource;

namespace Topaz.Service.ResourceManager;

internal sealed class ResourceManagerControlPlane(
    ResourceManagerResourceProvider provider,
    TemplateDeploymentOrchestrator templateDeploymentOrchestrator)
{
    private const string DeploymentNotFoundMessageTemplate = "Deployment {0} not found";
    private const string DeploymentNotFoundCode = "DeploymentNotFound";

    private readonly ArmTemplateEngineFacade _templateEngineFacade = new();

    public (OperationResult result, DeploymentResource resource) CreateOrUpdateDeployment(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string deploymentName, string content, string location, string deploymentMode)
    {
        var template = _templateEngineFacade.Parse(content);
        var deploymentResource = new DeploymentResource(subscriptionIdentifier, resourceGroupIdentifier, deploymentName,
            location, DeploymentResourceProperties.New(deploymentMode, template));

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, deploymentName, deploymentResource);

        var resourceGroupMetadata =
            new ResourceGroupMetadata(subscriptionIdentifier, resourceGroupIdentifier, location);
        var metadata = new DeploymentMetadata
        {
            { DeploymentMetadata.SubscriptionKey, subscriptionIdentifier.Value },
            { DeploymentMetadata.ResourceGroupKey,  JToken.Parse(resourceGroupMetadata.ToString())}
        };

        var metadataInsensitive =
            metadata.ToInsensitiveDictionary(meta => meta.Key, meta => meta.Value);

        templateDeploymentOrchestrator.EnqueueTemplateDeployment(subscriptionIdentifier, resourceGroupIdentifier,
            template, deploymentResource, metadataInsensitive);

        return (OperationResult.Success, deploymentResource);
    }

    public ControlPlaneOperationResult<DeploymentResource> GetDeployment(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string deploymentName)
    {
        var resource =
            provider.GetAs<DeploymentResource>(subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
        if (resource == null ||
            !resource.IsInSubscription(subscriptionIdentifier) ||
            !resource.IsInResourceGroup(resourceGroupIdentifier))
        {
            return new ControlPlaneOperationResult<DeploymentResource>(OperationResult.NotFound, null,
                string.Format(DeploymentNotFoundMessageTemplate, deploymentName), DeploymentNotFoundCode);
        }

        return new ControlPlaneOperationResult<DeploymentResource>(OperationResult.Success, resource, null, null);
    }

    public OperationResult DeleteDeployment(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string deploymentName)
    {
        var resource =
            provider.GetAs<DeploymentResource>(subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
        if (resource == null ||
            !resource.IsInSubscription(subscriptionIdentifier) ||
            !resource.IsInResourceGroup(resourceGroupIdentifier))
        {
            return OperationResult.NotFound;
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
        return OperationResult.Deleted;
    }

    public (OperationResult result, DeploymentResource[] resource) GetDeployments(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources = provider.ListAs<DeploymentResource>(subscriptionIdentifier, resourceGroupIdentifier, null, 8);

        var filteredBySubscriptionAndResourceGroup = resources.Where(deployment =>
                deployment.IsInSubscription(subscriptionIdentifier) &&
                deployment.IsInResourceGroup(resourceGroupIdentifier))
            .ToArray();

        return (OperationResult.Success, filteredBySubscriptionAndResourceGroup);
    }

    public ControlPlaneOperationResult<DeploymentValidateResult> ValidateDeployment(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string deploymentName, string input)
    {
        var deploymentOperation = GetDeployment(subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
        if (deploymentOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<DeploymentValidateResult>(OperationResult.Failed,
                new DeploymentValidateResult
                {
                    Error = new GenericErrorResponse(deploymentOperation.Code!, deploymentOperation.Reason!)
                }, deploymentOperation.Reason, deploymentOperation.Code);
        }

        _templateEngineFacade.Validate(deploymentOperation.Resource!.AsTemplate());

        return new ControlPlaneOperationResult<DeploymentValidateResult>(OperationResult.Success,
            new DeploymentValidateResult
            {
                Name = deploymentName,
                Properties = deploymentOperation.Resource!.Properties
            }, null, null);
    }
}