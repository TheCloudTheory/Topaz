using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Deployments.Core.Definitions.Schema;
using Azure.ResourceManager.Resources.Models;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models;

public sealed class DeploymentResource
    : ArmResource<DeploymentResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public DeploymentResource()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public DeploymentResource(SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        AzureLocation location,
        DeploymentResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Resources/deployments/{name}";
        Name = name;
        Location = location;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.Resources/deployments";
    public override string Location { get; init; }
    public override IDictionary<string, string> Tags => new Dictionary<string, string>();
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public override DeploymentResourceProperties Properties { get; init; }

    public Template AsTemplate()
    {
        return JsonSerializer.Deserialize<Template>(JsonSerializer.Serialize(this, GlobalSettings.JsonOptions))!;
    }

    public void CompleteDeployment()
    {
        Properties.ProvisioningState = ResourcesProvisioningState.Succeeded.ToString();
    }

    public void FailDeployment()
    {
        Properties.ProvisioningState = ResourcesProvisioningState.Failed.ToString();
    }
}