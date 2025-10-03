using System.Text.Json.Serialization;
using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ResourceManager.Models;

internal sealed class DeploymentResource
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
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/Microsoft.Resources/deployments/{name}";
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
}