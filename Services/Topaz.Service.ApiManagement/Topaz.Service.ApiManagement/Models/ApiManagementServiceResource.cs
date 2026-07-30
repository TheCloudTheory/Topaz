using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class ApiManagementServiceResource : ArmResource<ApiManagementServiceResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public ApiManagementServiceResource()
#pragma warning restore CS8618
    {
    }

    public ApiManagementServiceResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        string location,
        IDictionary<string, string>? tags,
        ResourceSku? sku,
        ApiManagementServiceResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.ApiManagement/service/{name}";
        Name = name;
        Location = location;
        Tags = tags ?? new Dictionary<string, string>();
        Sku = sku;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.ApiManagement/service";
    public override string? Location { get; set; }
    public override IDictionary<string, string>? Tags { get; set; }
    public override ResourceSku? Sku { get; set; }
    public override string? Kind { get; init; }
    public override ApiManagementServiceResourceProperties Properties { get; init; }

    public void UpdateFromRequest(CreateOrUpdateApiManagementServiceRequest request)
    {
        Sku = request.Sku;
        Properties.PublisherEmail = request.Properties.PublisherEmail ?? Properties.PublisherEmail;
        Properties.PublisherName = request.Properties.PublisherName ?? Properties.PublisherName;
        Properties.NotificationSenderEmail = request.Properties.NotificationSenderEmail ?? Properties.NotificationSenderEmail;
        Properties.VirtualNetworkType = request.Properties.VirtualNetworkType ?? Properties.VirtualNetworkType;
        Properties.PublicNetworkAccess = request.Properties.PublicNetworkAccess ?? Properties.PublicNetworkAccess;
        Properties.NatGatewayState = request.Properties.NatGatewayState ?? Properties.NatGatewayState;
        Properties.DisableGateway = request.Properties.DisableGateway ?? Properties.DisableGateway;
        Properties.EnableClientCertificate = request.Properties.EnableClientCertificate ?? Properties.EnableClientCertificate;
    }
}