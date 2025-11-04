using Azure.Deployments.Core.Definitions.Schema;
using Azure.Deployments.Core.Entities;
using DeploymentResource = Topaz.Service.ResourceManager.Models.DeploymentResource;

namespace Topaz.Service.ResourceManager.Deployment;

internal sealed class TemplateDeployment
{
    public DeploymentStatus Status { get; private set; } = DeploymentStatus.New;
    public Template Template { get; }
    public DeploymentResource Deployment { get; }

    public TemplateDeployment(Template template, DeploymentResource deployment)
    {
        Template = template;
        Deployment = deployment;

        HydratePropertiesOfResources();
    }

    private void HydratePropertiesOfResources()
    {
        foreach (var resource in Template.Resources)
        {
            resource.Id = new TemplateGenericProperty<string>()
            {
                Value =
                    $"/subscriptions/{Deployment.GetSubscription()}/resourceGroups/{Deployment.GetResourceGroup()}/providers/{resource.Type}/{resource.Name}"
            };
        }
    }

    public void Start()
    {
        Status = DeploymentStatus.Running;
    }
    
    public void Complete()
    {
        Status = DeploymentStatus.Completed;
        
        Deployment.CompleteDeployment();
    }
    
    public enum DeploymentStatus
    {
        New,
        Running,
        Completed,
        Cancelled,
        Failed,
    }

    public void Fail()
    {
        Status = DeploymentStatus.Failed;
        
        Deployment.FailDeployment();
    }
}