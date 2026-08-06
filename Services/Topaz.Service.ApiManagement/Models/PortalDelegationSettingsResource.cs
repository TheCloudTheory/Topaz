using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class PortalDelegationSettingsResource : ArmSubresource<PortalDelegationSettingsResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public PortalDelegationSettingsResource()
#pragma warning restore CS8618
    {
    }

    public PortalDelegationSettingsResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string parentName,
        PortalDelegationSettingsResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.ApiManagement/service/{parentName}/portalsettings/delegation";
        Name = "signin";
        Properties = properties;
        ETag = ContractEtag.New();
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.ApiManagement/service/portalsettings";
    public override PortalDelegationSettingsResourceProperties Properties { get; init; }
    
    [JsonIgnore]
    public ContractEtag? ETag { get; set; }

    public void UpdateFromRequest(CreateOrUpdatePortalDelegationSettingsRequest request)
    {
        Properties.Subscriptions?.Enabled = request.Properties?.Subscriptions?.Enabled ?? false;
        Properties.Url = request.Properties?.Url;
        Properties.UserRegistration?.Enabled = request.Properties?.UserRegistration?.Enabled ?? false;
        Properties.ValidationKey = request.Properties?.ValidationKey;
    }
}