using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Newtonsoft.Json.Linq;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.ResourceManager.Models;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

internal sealed class SubscriptionDeploymentControlPlane(
    SubscriptionDeploymentResourceProvider provider,
    TemplateDeploymentOrchestrator orchestrator,
    ITopazLogger logger)
{
    private const string DeploymentNotFoundMessageTemplate = "Deployment {0} not found";
    private const string DeploymentNotFoundCode = "DeploymentNotFound";

    // Segment depth: .topaz / .subscription / {subId} / .resource-manager / {name} / metadata.json = 6
    private const uint SubscriptionDeploymentDepth = 6;

    private readonly ArmTemplateEngineFacade _templateEngineFacade = new();

    public ControlPlaneOperationResult<SubscriptionDeploymentResource> CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier,
        string deploymentName,
        string content,
        Dictionary<string, CreateDeploymentRequest.ParameterValue>? parameters,
        string location,
        string mode)
    {
        var template = _templateEngineFacade.Parse(content);
        var deploymentResource = new SubscriptionDeploymentResource(
            subscriptionIdentifier,
            deploymentName,
            location,
            DeploymentResourceProperties.New(mode, content, parameters));

        provider.CreateOrUpdate(subscriptionIdentifier, null, deploymentName, deploymentResource);

        var subscriptionMetadata = new SubscriptionMetadata(subscriptionIdentifier);
        var metadataInsensitive = new Dictionary<string, JToken>
            {
                { DeploymentMetadata.SubscriptionKey, JToken.Parse(subscriptionMetadata.ToString()) }
            }
            .ToInsensitiveDictionary(meta => meta.Key, meta => meta.Value);

        orchestrator.EnqueueSubscriptionDeployment(subscriptionIdentifier, template, deploymentResource,
            metadataInsensitive);

        return new ControlPlaneOperationResult<SubscriptionDeploymentResource>(
            OperationResult.Success, deploymentResource, null, null);
    }

    public OperationResult CancelDeployment(SubscriptionIdentifier subscriptionIdentifier, string deploymentName)
    {
        var deploymentOp = Get(subscriptionIdentifier, deploymentName);
        if (deploymentOp.Result == OperationResult.NotFound)
            return OperationResult.NotFound;

        var provisioningState = deploymentOp.Resource!.Properties.ProvisioningState;
        if (provisioningState != Azure.ResourceManager.Resources.Models.ResourcesProvisioningState.Created.ToString())
            return OperationResult.Conflict;

        return orchestrator.CancelDeployment(
            $"/subscriptions/{subscriptionIdentifier}/providers/Microsoft.Resources/deployments/{deploymentName}");
    }

    public ControlPlaneOperationResult<SubscriptionDeploymentResource> Get(
        SubscriptionIdentifier subscriptionIdentifier,
        string deploymentName)
    {
        logger.LogDebug(nameof(SubscriptionDeploymentControlPlane), nameof(Get),
            "Getting subscription-scope deployment `{0}` in subscription {1}.", deploymentName,
            subscriptionIdentifier.Value);

        var resource = provider.GetAs<SubscriptionDeploymentResource>(subscriptionIdentifier, null, deploymentName);
        if (resource == null || !resource.IsInSubscription(subscriptionIdentifier))
        {
            return new ControlPlaneOperationResult<SubscriptionDeploymentResource>(
                OperationResult.NotFound, null,
                string.Format(DeploymentNotFoundMessageTemplate, deploymentName),
                DeploymentNotFoundCode);
        }

        return new ControlPlaneOperationResult<SubscriptionDeploymentResource>(
            OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<SubscriptionDeploymentResource[]> List(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        logger.LogDebug(nameof(SubscriptionDeploymentControlPlane), nameof(List),
            "Listing subscription-scope deployments for subscription {0}.", subscriptionIdentifier.Value);

        var resources = provider.ListAs<SubscriptionDeploymentResource>(
            subscriptionIdentifier, null, null, SubscriptionDeploymentDepth);

        var filtered = resources
            .Where(d => d.IsInSubscription(subscriptionIdentifier))
            .ToArray();

        return new ControlPlaneOperationResult<SubscriptionDeploymentResource[]>(
            OperationResult.Success, filtered, null, null);
    }
}
