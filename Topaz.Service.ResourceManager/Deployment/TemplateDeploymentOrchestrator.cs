using System.Text.Json;
using Azure.Deployments.Core.Definitions.Schema;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Newtonsoft.Json.Linq;
using Topaz.ResourceManager;
using Topaz.Service.EventHub;
using Topaz.Service.KeyVault;
using Topaz.Service.ManagedIdentity;
using Topaz.Service.ResourceManager.Models;
using Topaz.Service.ServiceBus;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.VirtualNetwork;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Deployment;

public sealed class TemplateDeploymentOrchestrator(ResourceManagerResourceProvider provider, ITopazLogger logger)
{
    private static Queue<TemplateDeployment> DeploymentQueue { get; } = new();
    private static Thread? OrchestratorThread { get; set; }

    private readonly ArmTemplateEngineFacade _armTemplateEngineFacade = new();
        
    public void EnqueueTemplateDeployment(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, Template template, DeploymentResource deploymentResource,
        InsensitiveDictionary<JToken> metadataInsensitive)
    {
        _armTemplateEngineFacade.ProcessTemplate(subscriptionIdentifier, resourceGroupIdentifier, template, metadataInsensitive, deploymentResource.Properties.Parameters);
        
        DeploymentQueue.Enqueue(new TemplateDeployment(template, deploymentResource));
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
                if (DeploymentQueue.Count == 0)
                {
                    logger.LogInformation("No deployments in the queue, will attempt to check again in 10 seconds...");
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    
                    continue;
                }
            
                logger.LogDebug(nameof(TemplateDeploymentOrchestrator), nameof(Start), "Attempting to dequeue a deployment from the queue...");
                var deployment = DeploymentQueue.Dequeue();
                logger.LogDebug(nameof(TemplateDeploymentOrchestrator), nameof(Start), "Fetched deployment for resource ID: {0}", deployment.Deployment.Id);

                RouteDeployment(deployment);
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
                case "Microsoft.KeyVault/vaults":
                    controlPlane = KeyVaultControlPlane.New(logger);
                    break;
                case "Microsoft.Network/virtualNetworks":
                    controlPlane = VirtualNetworkControlPlane.New(logger);
                    break;
                case "Microsoft.ManagedIdentity/userAssignedIdentities":
                    controlPlane = ManagedIdentityControlPlane.New(logger);
                    break;
                case "Microsoft.EventHub/namespaces":
                    controlPlane = EventHubServiceControlPlane.New(logger);
                    break;
                case "Microsoft.ServiceBus/namespaces":
                    controlPlane = ServiceBusServiceControlPlane.New(logger);
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