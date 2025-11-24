using System.Text.Json;
using Azure.Deployments.Core.Definitions.Schema;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Newtonsoft.Json.Linq;
using Topaz.ResourceManager;
using Topaz.Service.KeyVault;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ResourceManager.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
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
        _armTemplateEngineFacade.ProcessTemplate(subscriptionIdentifier, resourceGroupIdentifier, template, metadataInsensitive);
        
        DeploymentQueue.Enqueue(new TemplateDeployment(template, deploymentResource));
    }

    public void Start()
    {
        if (OrchestratorThread != null)
        {
            throw new InvalidOperationException("Orchestrator thread already running");    
        }
        
        OrchestratorThread = new Thread(() =>
        {
            while (true)
            {
                if (DeploymentQueue.Count == 0)
                {
                    logger.LogInformation("No deployments in the queue, will attempt to check again in 10 seconds...");
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    
                    continue;
                }
            
                logger.LogDebug("Attempting to dequeue a deployment from the queue...");
                var deployment = DeploymentQueue.Dequeue();
                logger.LogDebug($"Fetched deployment for resource ID: {deployment.Deployment.Id}");

                RouteDeployment(deployment);
            }
        });
        
        OrchestratorThread.Start();
    }

    private void RouteDeployment(TemplateDeployment templateDeployment)
    {
        logger.LogDebug($"Routing deployment resources of {templateDeployment.Deployment.Id} deployment to appropriate control planes.");

        var hasProvisioningFailed = false;
        foreach (var resource in templateDeployment.Template.Resources)
        {
            IControlPlane? controlPlane = null;
            var genericResource = JsonSerializer.Deserialize<GenericResource>(resource.ToJson(), GlobalSettings.JsonOptions)!;
            
            switch (resource.Type.Value)
            {
                case "Microsoft.KeyVault/vaults":
                    controlPlane = new KeyVaultControlPlane(new KeyVaultResourceProvider(logger),
                        new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger),
                            new SubscriptionControlPlane(new SubscriptionResourceProvider(logger)), logger),
                        new SubscriptionControlPlane(new SubscriptionResourceProvider(logger)), logger);
                    break;
                default:
                    logger.LogWarning($"Deployment of {templateDeployment.Deployment.Type} is not yet supported.");
                    break;
            }

            templateDeployment.Start();
            var result = controlPlane?.Deploy(genericResource);

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