using JetBrains.Annotations;
using Topaz.Service.ApiManagement.Models.Requests;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class ApiContractResourceProperties
{
    public string? ApiRevision { get; init; }
    public string? ApiRevisionDescription { get; init; }
    public string? ApiVersion { get; init; }
    public string? ApiVersionDescription { get; init; }
    public string? ApiVersionSetId { get; init; }
    public AuthenticationSettingsContract? AuthenticationSettings { get; init; }
    public ApiContactInformation? Contact { get; init; }
    public string? Description { get; init; }
    public bool? IsCurrent { get; init; }
    public ApiLicenseInformation? License { get; init; }
    public SubscriptionKeyParameterNamesContract? SubscriptionKeyParameterNames { get; init; }
    public bool? SubscriptionRequired { get; init; }
    public string? TermsOfServiceUrl { get; init; }
    public string? Type { get; init; }
    public string? Path { get; init; }
    public string? ApiType { get; init; }
    public ApiVersionSetContractDetails? ApiVersionSet { get; init; }
    public string? DisplayName { get; init; }
    public string? Format { get; init; }
    public string[]? Protocols { get; init; }
    public string? ServiceUrl { get; init; }
    public string? SourceApiId { get; init; }
    public string? TranslateRequiredQueryParameters { get; init; }
    public string? Value { get; init; }
    public WsdlSelector? WsdlSelector { get; init; }

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