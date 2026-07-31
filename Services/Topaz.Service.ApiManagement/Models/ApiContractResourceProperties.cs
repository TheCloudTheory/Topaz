using JetBrains.Annotations;
using Topaz.Service.ApiManagement.Models.Requests;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class ApiContractResourceProperties
{
    public string? ApiRevision { get; set; }
    public string? ApiRevisionDescription { get; set; }
    public string? ApiVersion { get; set; }
    public string? ApiVersionDescription { get; set; }
    public string? ApiVersionSetId { get; set; }
    public AuthenticationSettingsContract? AuthenticationSettings { get; set; }
    public ApiContactInformation? Contact { get; set; }
    public string? Description { get; set; }
    public bool? IsCurrent { get; set; }
    public ApiLicenseInformation? License { get; set; }
    public SubscriptionKeyParameterNamesContract? SubscriptionKeyParameterNames { get; set; }
    public bool? SubscriptionRequired { get; set; }
    public string? TermsOfServiceUrl { get; set; }
    public string? Type { get; set; }
    public string? Path { get; set; }
    public string? ApiType { get; set; }
    public ApiVersionSetContractDetails? ApiVersionSet { get; set; }
    public string? DisplayName { get; set; }
    public string? Format { get; set; }
    public string[]? Protocols { get; set; }
    public string? ServiceUrl { get; set; }
    public string? SourceApiId { get; set; }
    public string? TranslateRequiredQueryParameters { get; set; }
    public string? Value { get; set; }
    public WsdlSelector? WsdlSelector { get; set; }

    public static ApiContractResourceProperties From(CreateOrUpdateApiRequest request)
    {
        return new ApiContractResourceProperties
        {
            ApiRevision = request.ApiRevision,
            ApiRevisionDescription = request.ApiRevisionDescription,
            ApiVersion = request.ApiVersion,
            ApiVersionDescription = request.ApiVersionDescription,
            ApiVersionSetId = request.ApiVersionSetId,
            AuthenticationSettings = request.AuthenticationSettings,
            Contact = request.Contact,
            Description = request.Description,
            IsCurrent = request.IsCurrent,
            License = request.License,
            SubscriptionKeyParameterNames = request.SubscriptionKeyParameterNames,
            SubscriptionRequired = request.SubscriptionRequired,
            TermsOfServiceUrl = request.TermsOfServiceUrl,
            Type = request.Type,
            Path = request.Properties?.Path,
            ApiType = request.Properties?.ApiType,
            ApiVersionSet = request.Properties?.ApiVersionSet,
            DisplayName = request.Properties?.DisplayName,
            Format = request.Properties?.Format,
            Protocols = request.Properties?.Protocols,
            ServiceUrl = request.Properties?.ServiceUrl,
            SourceApiId = request.Properties?.SourceApiId,
            TranslateRequiredQueryParameters = request.Properties?.TranslateRequiredQueryParameters,
            Value = request.Properties?.Value,
            WsdlSelector = request.Properties?.WsdlSelector,
        };
    }
}

[UsedImplicitly]
internal sealed class AuthenticationSettingsContract
{
    public OAuth2AuthenticationSettingsContract? OAuth2 { get; init; }
    public OpenIdAuthenticationSettingsContract? OpenId { get; init; }
}

[UsedImplicitly]
internal sealed class OAuth2AuthenticationSettingsContract
{
    public string? AuthorizationServerId { get; init; }
    public string? Scope { get; init; }
}

[UsedImplicitly]
internal sealed class OpenIdAuthenticationSettingsContract
{
    public string? OpenIdProviderId { get; init; }
    public string[]? BearerTokenSendingMethods { get; init; }
}

[UsedImplicitly]
internal sealed class ApiContactInformation
{
    public string? Name { get; init; }
    public string? Url { get; init; }
    public string? Email { get; init; }
}

[UsedImplicitly]
internal sealed class ApiLicenseInformation
{
    public string? Name { get; init; }
    public string? Url { get; init; }
}

[UsedImplicitly]
internal sealed class ApiVersionSetContractDetails
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? VersioningScheme { get; init; }
    public string? VersionQueryName { get; init; }
    public string? VersionHeaderName { get; init; }
}

[UsedImplicitly]
internal sealed class WsdlSelector
{
    public string? WsdlServiceName { get; init; }
    public string? WsdlEndpointName { get; init; }
}

[UsedImplicitly]
internal sealed class SubscriptionKeyParameterNamesContract
{
    public string? Header { get; init; }
    public string? Query { get; init; }
}