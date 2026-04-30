using Azure.Core;
using Azure.ResourceManager.Resources.Models;
using Topaz.ResourceManager;
using Topaz.Service.ResourceManager.Models;

namespace Topaz.Service.ResourceManager.Models;

public sealed class TenantDeploymentResource : ArmResource<DeploymentResourceProperties>
{
    public TenantDeploymentResource() { }

    public TenantDeploymentResource(
        string name,
        AzureLocation location,
        DeploymentResourceProperties properties)
    {
        Id = $"/providers/Microsoft.Resources/deployments/{name}";
        Name = name;
        Location = location;
        Properties = properties;
    }

    public override string Id { get; init; } = string.Empty;
    public override string Name { get; init; } = string.Empty;
    public override string Type { get; init; } = "Microsoft.Resources/deployments";
    public override string? Location { get; set; }
    public override IDictionary<string, string>? Tags { get; set; } = new Dictionary<string, string>();
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public override DeploymentResourceProperties Properties { get; init; } = new();

    public void CompleteDeployment() =>
        Properties.ProvisioningState = ResourcesProvisioningState.Succeeded.ToString();

    public void FailDeployment() =>
        Properties.ProvisioningState = ResourcesProvisioningState.Failed.ToString();

    public void CancelDeployment() =>
        Properties.ProvisioningState = ResourcesProvisioningState.Canceled.ToString();
}
