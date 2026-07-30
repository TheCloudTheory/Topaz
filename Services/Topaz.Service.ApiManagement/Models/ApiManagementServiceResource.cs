using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class ApiManagementServiceResource : ArmResource<ApiManagementServiceResourceProperties>, IValidatable
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

    private static readonly Regex NamePattern = new(@"^[a-zA-Z](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?$", RegexOptions.Compiled);

    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length < 1 || Name.Length > 50 || !NamePattern.IsMatch(Name))
        {
            return (false,
                "Name must be 1-50 characters, start with a letter, end with a letter or digit, and contain only letters, digits, or hyphens.");
        }

        if (string.IsNullOrWhiteSpace(Location))
        {
            return (false, "Location cannot be null or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(Properties.PublisherEmail))
        {
            return (false, "Publisher email cannot be null or whitespace.");
        }
        
        if (string.IsNullOrWhiteSpace(Properties.PublisherName))
        {
            return (false, "Publisher name cannot be null or whitespace.");
        }
        
        if(Properties.PublisherEmail.Length > 100)
        {
            return (false, "Publisher email cannot be longer than 100 characters.");
        }
        
        if(Properties.PublisherName.Length > 100)
        {
            return (false, "Publisher name cannot be longer than 100 characters.");
        }

        return Sku == null ? (false, "Sku cannot be null.") : (true, null);
    }
    
    public static bool CheckIfNameIsValid(string name)
    {
        return string.IsNullOrWhiteSpace(name) || name.Length < 1 || name.Length > 50 || !NamePattern.IsMatch(name);
    }
}