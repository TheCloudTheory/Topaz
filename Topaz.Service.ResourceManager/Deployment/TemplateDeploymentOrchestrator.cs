using System.Text.Json;
using Azure.Deployments.Core.Definitions.Schema;
using Topaz.ResourceManager;
using Topaz.Service.ResourceManager.Models;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Deployment;

public sealed class TemplateDeploymentOrchestrator(ResourceManagerResourceProvider provider, ITopazLogger logger)
{
    private static Queue<TemplateDeployment> DeploymentQueue { get; } = new();
    private static Thread? OrchestratorThread { get; set; }

    private readonly ArmTemplateEngineFacade _armTemplateEngineFacade = new();
        
    public void EnqueueTemplateDeployment(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, Template template, DeploymentResource deploymentResource)
    {
        _armTemplateEngineFacade.ProcessTemplate(subscriptionIdentifier, resourceGroupIdentifier, template);
        
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
        logger.LogDebug($"Routing deployment resources of {templateDeployment.Deployment.Id} deployment to appriopriate control planes.");
        
        foreach (var resource in templateDeployment.Template.Resources)
        {
            var genericResource = JsonSerializer.Deserialize<GenericResource>(resource.ToJson(), GlobalSettings.JsonOptions);
            
            switch (resource.Type.Value)
            {
                case "Microsoft.KeyVault/vaults":
                    break;
                default:
                    logger.LogWarning($"Deployment of {templateDeployment.Deployment.Type} is not yet supported.");
                    break;
            }
        }

        templateDeployment.Deployment.CompleteDeployment();
        provider.CreateOrUpdate(templateDeployment.Deployment.GetSubscription(), templateDeployment.Deployment.GetResourceGroup(), templateDeployment.Deployment.Name, templateDeployment.Deployment);
        logger.LogInformation($"Deployment {templateDeployment.Deployment.Id} completed.");
    }
}