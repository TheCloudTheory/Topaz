using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ContainerRegistry.Models;

internal sealed class AcrTaskResource : ArmSubresource<AcrTaskResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public AcrTaskResource()
#pragma warning restore CS8618
    {
    }

    public AcrTaskResource(
        SubscriptionIdentifier subscription,
        ResourceGroupIdentifier resourceGroup,
        string registryName,
        string taskName,
        string location,
        IDictionary<string, string>? tags,
        JsonElement? identity,
        AcrTaskResourceProperties properties)
    {
        Id = $"/subscriptions/{subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.ContainerRegistry/registries/{registryName}/tasks/{taskName}";
        Name = taskName;
        Location = location;
        Tags = tags;
        Identity = identity;
        Properties = properties;
    }

    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.ContainerRegistry/registries/tasks";
    public override AcrTaskResourceProperties Properties { get; init; }
    public string? Location { get; init; }
    public IDictionary<string, string>? Tags { get; set; }
    public JsonElement? Identity { get; set; }
}
