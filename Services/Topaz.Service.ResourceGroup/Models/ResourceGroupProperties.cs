using Topaz.Service.ResourceGroup.Models.Requests;

namespace Topaz.Service.ResourceGroup.Models;

public sealed class ResourceGroupProperties
{
    public string ProvisioningState => "Succeeded";

    public static void UpdateFromRequest(ResourceGroupResource resource, CreateOrUpdateResourceGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(resource);

        resource.Tags = request.Tags;
    }
}