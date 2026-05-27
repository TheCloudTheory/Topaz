using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ContainerRegistry.Models;

internal sealed class AcrRunResource : ArmSubresource<AcrRunResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public AcrRunResource()
#pragma warning restore CS8618
    {
    }

    public AcrRunResource(
        SubscriptionIdentifier subscription,
        ResourceGroupIdentifier resourceGroup,
        string registryName,
        string runId,
        AcrRunResourceProperties properties)
    {
        Id = $"/subscriptions/{subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.ContainerRegistry/registries/{registryName}/runs/{runId}";
        Name = runId;
        Properties = properties;
    }

    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.ContainerRegistry/registries/runs";
    public override AcrRunResourceProperties Properties { get; init; }
}
