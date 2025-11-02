using Azure.Deployments.Core.Definitions.Schema;
using Topaz.Service.ResourceManager.Models;

namespace Topaz.Service.ResourceManager.Deployment;

internal sealed class TemplateDeployment(Template template, DeploymentResource deployment)
{
    public DeploymentStatus Status { get; private set; } = DeploymentStatus.New;
    public Template Template { get; } = template;
    public DeploymentResource Deployment { get; } = deployment;

    public void StartDeployment()
    {
        Status =  DeploymentStatus.Running;
    }
    
    public enum DeploymentStatus
    {
        New,
        Running,
        Completed,
        Cancelled,
        Failed,
    }
}