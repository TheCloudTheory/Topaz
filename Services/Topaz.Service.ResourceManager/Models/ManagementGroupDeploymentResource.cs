using Azure.Core;
using Azure.ResourceManager.Resources.Models;
using Topaz.ResourceManager;
using Topaz.Service.ResourceManager.Models;

namespace Topaz.Service.ResourceManager.Models;

public sealed class ManagementGroupDeploymentResource : ArmResource<DeploymentResourceProperties>
{
    public ManagementGroupDeploymentResource() { }

    public ManagementGroupDeploymentResource(
        string groupId,
        string name,
        AzureLocation location,
        DeploymentResourceProperties properties)
    {
        Id = $"/providers/Microsoft.Management/managementGroups/{groupId}/providers/Microsoft.Resources/deployments/{name}";
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

    /// <summary>
    /// Returns true when this deployment's Id belongs to the specified management group.
    /// </summary>
    public bool IsInManagementGroup(string groupId)
    {
        if (string.IsNullOrEmpty(Id)) return false;
        var segments = Id.Split('/');
        // /providers/Microsoft.Management/managementGroups/{groupId}/providers/...
        // idx: 0=""  1="providers"  2="Microsoft.Management"  3="managementGroups"  4="{groupId}"
        return segments.Length > 4 &&
               string.Equals(segments[4], groupId, StringComparison.OrdinalIgnoreCase);
    }
}
