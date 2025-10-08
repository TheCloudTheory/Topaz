namespace Topaz.Service.ResourceManager.Models.Requests;

internal record CreateDeploymentRequest
{
    public DeploymentProperties? Properties { get; set; }

    internal record DeploymentProperties
    {
        public string? Mode { get; set; }
    }
}