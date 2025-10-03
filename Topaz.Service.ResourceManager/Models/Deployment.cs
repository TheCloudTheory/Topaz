namespace Topaz.Service.ResourceManager.Models;

internal record Deployment
{
    public DeploymentProperties? Properties { get; set; }

    internal record DeploymentProperties
    {
        public string? Mode { get; set; }
    }
}