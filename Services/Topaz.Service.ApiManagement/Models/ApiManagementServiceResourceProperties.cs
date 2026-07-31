using Topaz.Service.ApiManagement.Models.Requests;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class ApiManagementServiceResourceProperties
{
    // required
    public string? PublisherEmail { get; set; }
    public string? PublisherName { get; set; }

    // read-only
    public string ProvisioningState { get; set; } = "Succeeded";
    public string? TargetProvisioningState { get; set; } = "";
    public DateTimeOffset? CreatedAtUtc { get; set; }
    public string? GatewayUrl { get; set; }
    public string? GatewayRegionalUrl { get; set; }
    public string? PortalUrl { get; set; }
    public string? DeveloperPortalUrl { get; set; }
    public string? ManagementApiUrl { get; set; }
    public string? ScmUrl { get; set; }
    public string[]? PublicIPAddresses { get; set; }
    public string[]? PrivateIPAddresses { get; set; }
    public string? PlatformVersion { get; set; }

    // optional writable
    public string? NotificationSenderEmail { get; set; }
    public string? VirtualNetworkType { get; set; }
    public string? PublicNetworkAccess { get; set; }
    public string? NatGatewayState { get; set; }
    public bool? DisableGateway { get; set; }
    public bool? EnableClientCertificate { get; set; }
    public IDictionary<string, string>? CustomProperties { get; init; }
    
    public static ApiManagementServiceResourceProperties From(CreateOrUpdateApiManagementServiceRequest request)
    {
        return new ApiManagementServiceResourceProperties
        {
            CreatedAtUtc = DateTimeOffset.UtcNow,
            PublisherEmail = request.Properties?.PublisherEmail,
            PublisherName = request.Properties?.PublisherName,
            NotificationSenderEmail = request.Properties?.NotificationSenderEmail,
            VirtualNetworkType = request.Properties?.VirtualNetworkType,
            PublicNetworkAccess = request.Properties?.PublicNetworkAccess,
            NatGatewayState = request.Properties?.NatGatewayState,
            DisableGateway = request.Properties?.DisableGateway,
            EnableClientCertificate = request.Properties?.EnableClientCertificate,
            CustomProperties = request.Properties?.CustomProperties
        };
    }
}