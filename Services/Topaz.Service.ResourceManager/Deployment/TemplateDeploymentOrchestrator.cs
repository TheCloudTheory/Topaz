using System.Text.Json;
using Azure.Deployments.Core.Definitions.Schema;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
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
using Topaz.Service.VirtualNetwork;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Deployment;

public sealed class TemplateDeploymentOrchestrator(Pipeline eventPipeline, ResourceManagerResourceProvider provider, ITopazLogger logger)
{
    private static readonly List<TemplateDeployment> DeploymentQueue = [];
    private static readonly object QueueLock = new();
    private static string? _currentDeploymentId;
    private static Thread? OrchestratorThread { get; set; }

    private readonly ArmTemplateEngineFacade _armTemplateEngineFacade = new();
        
    public void EnqueueTemplateDeployment(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, Template template, DeploymentResource deploymentResource,
        InsensitiveDictionary<JToken> metadataInsensitive)
    {
        _armTemplateEngineFacade.ProcessTemplate(subscriptionIdentifier, resourceGroupIdentifier, template, metadataInsensitive, deploymentResource.Properties.Parameters);
        
        lock (QueueLock)
        {
            DeploymentQueue.Add(new TemplateDeployment(template, deploymentResource));
        }
    }

    public OperationResult CancelDeployment(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string deploymentName)
    {
        var deploymentId =
            $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/Microsoft.Resources/deployments/{deploymentName}";

        TemplateDeployment? toCancel;
        lock (QueueLock)
        {
            if (_currentDeploymentId == deploymentId)
                return OperationResult.Conflict;

            toCancel = DeploymentQueue.FirstOrDefault(d => d.Deployment.Id == deploymentId);
            if (toCancel == null)
                return OperationResult.Conflict;

            DeploymentQueue.RemoveAll(d => d.Deployment.Id == deploymentId);
        }

        toCancel.Cancel();
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, deploymentName, toCancel.Deployment);
        return OperationResult.Success;
    }

    public void Start(CancellationToken stoppingToken = default)
    {
        if (OrchestratorThread != null)
        {
            throw new InvalidOperationException("Orchestrator thread already running");    
        }
        
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
                        _currentDeploymentId = deployment.Deployment.Id;
                    }
                }

                if (deployment == null)
                {
                    logger.LogInformation("No deployments in the queue, will attempt to check again in 10 seconds...");
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    
                    continue;
                }

                logger.LogDebug(nameof(TemplateDeploymentOrchestrator), nameof(Start), "Fetched deployment for resource ID: {0}", deployment.Deployment.Id);

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
        logger.LogDebug(nameof(TemplateDeploymentOrchestrator), nameof(RouteDeployment), "Routing deployment resources of {0} deployment to appropriate control planes.", templateDeployment.Deployment.Id);

        templateDeployment.Start();
        logger.LogInformation($"Deployment of {templateDeployment.Deployment.Id} started.");
        
        var hasProvisioningFailed = false;
        foreach (var resource in templateDeployment.Template.Resources)
        {
            IControlPlane? controlPlane = null;
            var genericResource = JsonSerializer.Deserialize<GenericResource>(resource.ToJson(), GlobalSettings.JsonOptions)!;
            
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
                    logger.LogWarning($"Deployment of {resource.Type} is not yet supported.");
                    break;
            }
            
            var result = controlPlane?.Deploy(genericResource);
            logger.LogInformation($"Deployment of {genericResource.Id} completed with status {result}.");

            if (result == OperationResult.Failed)
            {
                hasProvisioningFailed = true;
            }
        }

        if (!hasProvisioningFailed)
        {
            templateDeployment.Complete();
        }
        else
        {
            templateDeployment.Fail();
        }
        
        provider.CreateOrUpdate(templateDeployment.Deployment.GetSubscription(), templateDeployment.Deployment.GetResourceGroup(), templateDeployment.Deployment.Name, templateDeployment.Deployment);
        logger.LogInformation($"Deployment {templateDeployment.Deployment.Id} completed.");
    }
}