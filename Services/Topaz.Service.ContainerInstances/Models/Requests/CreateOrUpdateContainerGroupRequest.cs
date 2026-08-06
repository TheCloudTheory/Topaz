namespace Topaz.Service.ContainerInstances.Models.Requests;

internal sealed class CreateOrUpdateContainerGroupRequest : ContainerInstancesServiceResource
{
    public static CreateOrUpdateContainerGroupRequest From(ContainerInstancesServiceResource aci)
    {
        return new CreateOrUpdateContainerGroupRequest
        {
            Location = aci.Location,
            Name = aci.Name,
            Tags = aci.Tags,
            Identity = aci.Identity,
            Sku = aci.Sku,
            Properties = aci.Properties
        };
    }
}