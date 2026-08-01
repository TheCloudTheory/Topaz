using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class ApiContractResource : ArmSubresource<ApiContractResourceProperties>, IValidatable
{
    [JsonConstructor]
#pragma warning disable CS8618
    public ApiContractResource()
#pragma warning restore CS8618
    {
    }

    public ApiContractResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string parentName,
        string name,
        ApiContractResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.ApiManagement/service/{parentName}/apis/{name}";
        Name = name;
        Properties = properties;
        ETag = ApiContractEtag.New();
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.ApiManagement/service/apis";
    public override ApiContractResourceProperties Properties { get; init; }
    
    [JsonIgnore]
    public ApiContractEtag? ETag { get; set; }
    
    private static readonly Regex NamePattern = new(@"^[^*#&+:<>?]+$", RegexOptions.Compiled);

    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        if (Name.Length is < 1 or > 256)
        {
            return (false, "Name must be between 1 and 256 characters");
        }

        if (!NamePattern.IsMatch(Name))
        {
            return (false, "Name cannot contain special characters such as ^*#&+:<>?");
        }
        
        if (Properties.Path == null)
            return (false, "properties.path is required");

        if (Properties.Path.Length > 400)
            return (false, "properties.path cannot exceed 400 characters");

        if (Properties.ApiRevision is { Length: 0 or > 100 })
            return (false, "apiRevision must be between 1 and 100 characters");

        if (Properties.ApiRevisionDescription?.Length > 256)
            return (false, "apiRevisionDescription cannot exceed 256 characters");

        if (Properties.ApiVersion?.Length > 100)
            return (false, "apiVersion cannot exceed 100 characters");

        if (Properties.ApiVersionDescription?.Length > 256)
            return (false, "apiVersionDescription cannot exceed 256 characters");

        if (Properties.DisplayName is { Length: 0 or > 300 })
            return (false, "properties.displayName must be between 1 and 300 characters");

        return Properties.ServiceUrl?.Length > 2000
            ? (false, "properties.serviceUrl cannot exceed 2000 characters")
            : (true, null);
    }

    public void UpdateFromRequest(CreateOrUpdateApiRequest request)
    {
        Properties.ApiRevision = request.ApiRevision ?? Properties.ApiRevision;
        Properties.ApiRevisionDescription = request.ApiRevisionDescription ?? Properties.ApiRevisionDescription;
        Properties.ApiVersion = request.ApiVersion ?? Properties.ApiVersion;
        Properties.ApiVersionDescription = request.ApiVersionDescription ?? Properties.ApiVersionDescription;
        Properties.ApiVersionSetId = request.ApiVersionSetId ?? Properties.ApiVersionSetId;
        Properties.AuthenticationSettings = request.AuthenticationSettings ?? Properties.AuthenticationSettings;
        Properties.Contact = request.Contact ?? Properties.Contact;
        Properties.Description = request.Description ?? Properties.Description;
        Properties.IsCurrent = request.IsCurrent ?? Properties.IsCurrent;
        Properties.License = request.License ?? Properties.License;
        Properties.SubscriptionKeyParameterNames = request.SubscriptionKeyParameterNames ?? Properties.SubscriptionKeyParameterNames;
        Properties.SubscriptionRequired = request.SubscriptionRequired ?? Properties.SubscriptionRequired;
        Properties.TermsOfServiceUrl = request.TermsOfServiceUrl ?? Properties.TermsOfServiceUrl;
        Properties.Type = request.Type ?? Properties.Type;
        Properties.Path = request.Properties?.Path ?? Properties.Path;
        Properties.ApiType = request.Properties?.ApiType ?? Properties.ApiType;
        Properties.ApiVersionSet = request.Properties?.ApiVersionSet ?? Properties.ApiVersionSet;
        Properties.DisplayName = request.Properties?.DisplayName ?? Properties.DisplayName;
        Properties.Format = request.Properties?.Format ?? Properties.Format;
        Properties.Protocols = request.Properties?.Protocols ?? Properties.Protocols;
        Properties.ServiceUrl = request.Properties?.ServiceUrl ?? Properties.ServiceUrl;
        Properties.SourceApiId = request.Properties?.SourceApiId ?? Properties.SourceApiId;
        Properties.TranslateRequiredQueryParameters = request.Properties?.TranslateRequiredQueryParameters ?? Properties.TranslateRequiredQueryParameters;
        Properties.Value = request.Properties?.Value ?? Properties.Value;
        Properties.WsdlSelector = request.Properties?.WsdlSelector ?? Properties.WsdlSelector;

        ETag = ApiContractEtag.New();
    }
}