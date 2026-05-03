using System.Text.Json;
using Azure.Deployments.Core.Definitions.Schema;
using Azure.Deployments.Core.Entities;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Newtonsoft.Json.Linq;
using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.ContainerRegistry;
using Topaz.Service.EventHub;
using Topaz.Service.KeyVault;
using Topaz.Service.ManagedIdentity;
using Topaz.Service.ResourceManager.Models;
using Topaz.Service.ServiceBus;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage;
using Topaz.Service.VirtualMachine;
using Topaz.Service.VirtualNetwork;
using Topaz.Shared;
using DeploymentResource = Topaz.Service.ResourceManager.Models.DeploymentResource;

namespace Topaz.Service.ResourceManager.Deployment;

public sealed class TemplateDeploymentOrchestrator(
    Pipeline eventPipeline,
    ResourceManagerResourceProvider rgProvider,
    SubscriptionDeploymentResourceProvider subProvider,
    ITopazLogger logger)
{
    private static readonly List<TemplateDeployment> DeploymentQueue = [];
    private static readonly object QueueLock = new();
    private static string? _currentDeploymentId;
    private static Thread? OrchestratorThread { get; set; }

    private readonly ArmTemplateEngineFacade _armTemplateEngineFacade = new();

    public void EnqueueTemplateDeployment(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        Template template,
        DeploymentResource deploymentResource,
        InsensitiveDictionary<JToken> metadataInsensitive)
    {
        _armTemplateEngineFacade.ProcessTemplate(subscriptionIdentifier, resourceGroupIdentifier, template,
            metadataInsensitive, deploymentResource.Properties.Parameters);

        foreach (var resource in template.Resources)
        {
            resource.Id = new TemplateGenericProperty<string>
            {
                Value = $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/{resource.Type}/{resource.Name}"
            };
        }

        var job = new TemplateDeployment(
            deploymentResource.Id, deploymentResource.Name, template,
            complete: deploymentResource.CompleteDeployment,
            cancel: deploymentResource.CancelDeployment,
            fail: deploymentResource.FailDeployment,
            persist: () => rgProvider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier,
                deploymentResource.Name, deploymentResource));

        lock (QueueLock) { DeploymentQueue.Add(job); }
    }

    public void EnqueueSubscriptionDeployment(
        SubscriptionIdentifier subscriptionIdentifier,
        Template template,
        SubscriptionDeploymentResource deploymentResource,
        InsensitiveDictionary<JToken> metadataInsensitive)
    {
        _armTemplateEngineFacade.ProcessTemplateAtSubscriptionScope(subscriptionIdentifier, template,
            metadataInsensitive, deploymentResource.Properties.Parameters);

        foreach (var resource in template.Resources)
        {
            resource.Id = new TemplateGenericProperty<string>
            {
                Value = $"/subscriptions/{subscriptionIdentifier}/providers/{resource.Type}/{resource.Name}"
            };
        }

        var job = new TemplateDeployment(
            deploymentResource.Id, deploymentResource.Name, template,
            complete: deploymentResource.CompleteDeployment,
            cancel: deploymentResource.CancelDeployment,
            fail: deploymentResource.FailDeployment,
            persist: () => subProvider.CreateOrUpdate(subscriptionIdentifier, null,
                deploymentResource.Name, deploymentResource));

        lock (QueueLock) { DeploymentQueue.Add(job); }
    }

    public OperationResult CancelDeployment(string deploymentId)
    {
        TemplateDeployment? toCancel;
        lock (QueueLock)
        {
            if (_currentDeploymentId == deploymentId)
                return OperationResult.Conflict;

            toCancel = DeploymentQueue.FirstOrDefault(d => d.Id == deploymentId);
            if (toCancel == null)
                return OperationResult.Conflict;

            DeploymentQueue.RemoveAll(d => d.Id == deploymentId);
        }

        toCancel.Cancel();
        toCancel.Persist();
        return OperationResult.Success;
    }

    public void Start(CancellationToken stoppingToken = default)
    {
        if (OrchestratorThread != null)
            throw new InvalidOperationException("Orchestrator thread already running");

        OrchestratorThread = new Thread(() =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TemplateDeployment? deployment = null;
                lock (QueueLock)
                {
                    if (DeploymentQueue.Count > 0)
                    {
                        deployment = DeploymentQueue[0];
                        DeploymentQueue.RemoveAt(0);
                        _currentDeploymentId = deployment.Id;
                    }
                }

                if (deployment == null)
                {
                    logger.LogInformation("No deployments in the queue, will attempt to check again in 10 seconds...");
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    continue;
                }

                logger.LogDebug(nameof(TemplateDeploymentOrchestrator), nameof(Start),
                    "Fetched deployment: {0}", deployment.Id);

                try
                {
                    RouteDeployment(deployment);
                }
                finally
                {
                    lock (QueueLock) { _currentDeploymentId = null; }
                }
            }
        });

        OrchestratorThread.Start();
    }

    private void RouteDeployment(TemplateDeployment templateDeployment)
    {
        logger.LogDebug(nameof(TemplateDeploymentOrchestrator), nameof(RouteDeployment),
            "Routing deployment resources of {0} to appropriate control planes.", templateDeployment.Id);

        templateDeployment.Start();
        logger.LogInformation($"Deployment of {templateDeployment.Id} started.");

        var hasProvisioningFailed = false;
        foreach (var resource in templateDeployment.Template.Resources)
        {
            IControlPlane? controlPlane = null;
            var genericResource =
                JsonSerializer.Deserialize<GenericResource>(resource.ToJson(), GlobalSettings.JsonOptions)!;

            switch (resource.Type.Value)
            {
                case "Microsoft.ContainerRegistry/registries":
                    controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.KeyVault/vaults":
                    controlPlane = KeyVaultControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.Network/virtualNetworks":
                    controlPlane = VirtualNetworkControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.Compute/virtualMachines":
                    controlPlane = VirtualMachineServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.ManagedIdentity/userAssignedIdentities":
                    controlPlane = ManagedIdentityControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.EventHub/namespaces":
                    controlPlane = EventHubServiceControlPlane.New(logger);
                    break;
                case "Microsoft.ServiceBus/namespaces":
                    controlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.Storage/storageAccounts":
                    controlPlane = AzureStorageControlPlane.New(logger);
                    break;
                default:
                    logger.LogWarning($"Deployment of resource type {resource.Type} is not yet supported.");
                    break;
            }

            var result = controlPlane?.Deploy(genericResource);
            logger.LogInformation($"Deployment of {genericResource.Id} completed with status {result}.");

            if (result == OperationResult.Failed)
                hasProvisioningFailed = true;
        }

        if (!hasProvisioningFailed)
            templateDeployment.Complete();
        else
            templateDeployment.Fail();

        templateDeployment.Persist();
        logger.LogInformation($"Deployment {templateDeployment.Id} completed.");
    }
}