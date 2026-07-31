using Topaz.ResourceManager;

namespace Topaz.Service.ApiManagement.Models.Requests;

internal sealed class CreateOrUpdateApiManagementServiceRequest
{
    public string? Location { get; init; }
    public ResourceSku? Sku { get; init; }
    public CreateOrUpdateApiManagementServiceRequestProperties? Properties { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
    public string[]? Zones { get; init; }

    public static CreateOrUpdateApiManagementServiceRequest From(ApiManagementServiceResource apim)
    {
        return new CreateOrUpdateApiManagementServiceRequest
        {
            Location = apim.Location!,
            Sku = apim.Sku!,
            Tags = apim.Tags,
            Properties = new CreateOrUpdateApiManagementServiceRequestProperties
            {
                CustomProperties = apim.Properties.CustomProperties,
                PublisherEmail = apim.Properties.PublisherEmail,
                PublisherName = apim.Properties.PublisherName,
                NotificationSenderEmail = apim.Properties.NotificationSenderEmail,
                VirtualNetworkType = apim.Properties.VirtualNetworkType,
                PublicNetworkAccess = apim.Properties.PublicNetworkAccess,
                NatGatewayState = apim.Properties.NatGatewayState,
                DisableGateway = apim.Properties.DisableGateway,
                EnableClientCertificate = apim.Properties.EnableClientCertificate,
            }
        };
    }
}

internal sealed class CreateOrUpdateApiManagementServiceRequestProperties
{
    public string? PublisherEmail { get; init; }
    public string? PublisherName { get; init; }
    public string? NotificationSenderEmail { get; init; }
    public string? VirtualNetworkType { get; init; }
    public string? PublicNetworkAccess { get; init; }
    public string? NatGatewayState { get; init; }
    public bool? DisableGateway { get; init; }
    public bool? EnableClientCertificate { get; init; }
    public bool? Restore { get; init; }
    public IDictionary<string, string>? CustomProperties { get; init; }
}