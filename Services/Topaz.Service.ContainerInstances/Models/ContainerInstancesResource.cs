using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.ContainerInstances.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ContainerInstances.Models;

internal class ContainerInstancesServiceResource : ArmResource<ContainerInstancesServiceResourceProperties>, IValidatable
{
    [JsonConstructor]
#pragma warning disable CS8618
    public ContainerInstancesServiceResource()
#pragma warning restore CS8618
    {
    }

    public ContainerInstancesServiceResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        string location,
        IDictionary<string, string>? tags,
        ResourceSku? sku,
        ContainerInstancesServiceResourceProperties properties)
    {
        Id =
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.ContainerInstances/containerGroups/{name}";
        Name = name;
        Location = location;
        Tags = tags ?? new Dictionary<string, string>();
        Sku = sku;
        Properties = properties;
    }

    public sealed override string Id { get; init; }
    public sealed override string Name { get; init; }
    public sealed override string Type { get; init; } = "Microsoft.ContainerInstances/containerGroups";
    public sealed override string? Location { get; set; }
    public sealed override IDictionary<string, string>? Tags { get; set; }
    public sealed override ResourceSku? Sku { get; set; }
    public override string? Kind { get; init; }
    public sealed override ContainerInstancesServiceResourceProperties Properties { get; init; }
    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        return (true, null);
    }

    public void UpdateFromRequest(CreateOrUpdateContainerGroupRequest request)
    {
        Sku = request.Sku ?? Sku;
        Properties.UpdateFromRequest(request);
    }
}