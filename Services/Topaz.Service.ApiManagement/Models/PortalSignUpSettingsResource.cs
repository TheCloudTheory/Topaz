using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class PortalSignUpSettingsResource : ArmSubresource<PortalSignUpSettingsResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public PortalSignUpSettingsResource()
#pragma warning restore CS8618
    {
    }

    public PortalSignUpSettingsResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string parentName,
        PortalSignUpSettingsResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.ApiManagement/service/{parentName}/portalsettings/signup";
        Name = "signin";
        Properties = properties;
        ETag = ContractEtag.New();
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.ApiManagement/service/portalsettings";
    public override PortalSignUpSettingsResourceProperties Properties { get; init; }
    
    [JsonIgnore]
    public ContractEtag? ETag { get; set; }

    public static PortalSignUpSettingsResourceProperties From(CreateOrUpdatePortalSignUpSettingsRequest request)
    {
        return new PortalSignUpSettingsResourceProperties
        {
            Enabled = request.Properties?.Enabled ?? false,
            TermsOfService = request.Properties?.TermsOfService
        };
    }

    public void UpdateFromRequest(CreateOrUpdatePortalSignUpSettingsRequest request)
    {
        Properties.Enabled = request.Properties?.Enabled ?? true;
        Properties.TermsOfService = request.Properties?.TermsOfService;
    }
}