using Azure.Deployments.Core.Definitions.Schema;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

public sealed class TemplateDeploymentOrchestrator(ITopazLogger logger)
{
    private static Queue<TemplateDeployment> DeploymentQueue { get; } = new();
    private static Thread? OrchestratorThread { get; set; }
        
    public void EnqueueTemplateDeployment(Template template)
    {
        foreach (var resource in template.Resources)
        {
            DeploymentQueue.Enqueue(new TemplateDeployment(resource));
        }
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
                    
                    return;
                }
            
                logger.LogDebug("Attempting to dequeue a deployment from the queue...");
                var deployment = DeploymentQueue.Dequeue();
                logger.LogDebug($"Fetched deployment for resource ID: {deployment.Resource.Id}");
            }
        });
        
        OrchestratorThread.Start();
    }
}

internal sealed class TemplateDeployment(TemplateResource resource)
{
    public DeploymentStatus Status { get; } = DeploymentStatus.New;
    
    public TemplateResource Resource { get; } = resource;
    
    public enum DeploymentStatus
    {
        New,
        Running,
        Completed,
        Cancelled,
        Failed,
    }
}