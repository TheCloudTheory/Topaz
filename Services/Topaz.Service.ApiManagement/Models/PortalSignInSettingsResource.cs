using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class PortalSignInSettingsResource : ArmSubresource<PortalSignInSettingsResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public PortalSignInSettingsResource()
#pragma warning restore CS8618
    {
    }

    public PortalSignInSettingsResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string parentName,
        PortalSignInSettingsResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.ApiManagement/service/{parentName}/portalsettings/signin";
        Name = "signin";
        Properties = properties;
        ETag = ContractEtag.New();
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.ApiManagement/service/portalsettings";
    public override PortalSignInSettingsResourceProperties Properties { get; init; }
    
    [JsonIgnore]
    public ContractEtag? ETag { get; set; }
    
    [JsonIgnore]
    public bool IsDefault { get; set; }

    public static PortalSignInSettingsResource Default => new()
    {
        IsDefault = true,
        Properties = new PortalSignInSettingsResourceProperties
        {
            Enabled = false
        }
    };

    public static PortalSignInSettingsResourceProperties From(CreateOrUpdatePortalSignInSettingsRequest request)
    {
        return new PortalSignInSettingsResourceProperties
        {
            Enabled = request.Properties?.Enabled ?? false
        };
    }

    public void UpdateFromRequest(CreateOrUpdatePortalSignInSettingsRequest request)
    {
        Properties.Enabled = request.Properties?.Enabled ?? true;
    }
}