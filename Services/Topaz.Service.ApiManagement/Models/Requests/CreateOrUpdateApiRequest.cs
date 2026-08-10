using Azure.ResourceManager.Resources;
using JetBrains.Annotations;
using Topaz.Service.ApiManagement.Models;

namespace Topaz.Service.ApiManagement.Models.Requests;

internal sealed class CreateOrUpdateApiRequest
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
    public CreateOrUpdateApiRequestProperties? Properties { get; init; }

    public static CreateOrUpdateApiRequest From(ApiContractResource api)
    {
        return new CreateOrUpdateApiRequest
        {
            ApiRevision = api.Properties.ApiRevision,
            ApiRevisionDescription = api.Properties.ApiRevisionDescription,
            ApiVersion = api.Properties.ApiVersion,
            ApiVersionDescription = api.Properties.ApiVersionDescription,
            ApiVersionSetId = api.Properties.ApiVersionSetId,
            AuthenticationSettings = api.Properties.AuthenticationSettings,
            Contact = api.Properties.Contact,
            Description = api.Properties.Description,
            IsCurrent = api.Properties.IsCurrent,
            License = api.Properties.License,
            SubscriptionKeyParameterNames = api.Properties.SubscriptionKeyParameterNames,
            SubscriptionRequired = api.Properties.SubscriptionRequired,
            TermsOfServiceUrl = api.Properties.TermsOfServiceUrl,
            Type = api.Properties.Type,
            Properties = new CreateOrUpdateApiRequestProperties
            {
                Path = api.Properties.Path,
                ApiType = api.Properties.ApiType,
                ApiVersionSet = api.Properties.ApiVersionSet,
                DisplayName = api.Properties.DisplayName,
                Format = api.Properties.Format,
                Protocols = api.Properties.Protocols,
                ServiceUrl = api.Properties.ServiceUrl,
                SourceApiId = api.Properties.SourceApiId,
                TranslateRequiredQueryParameters = api.Properties.TranslateRequiredQueryParameters,
                Value = api.Properties.Value,
                WsdlSelector = api.Properties.WsdlSelector
            }
        };
    }

    public static CreateOrUpdateApiRequest From(GenericResourceData data)
    {
        var properties = data.Properties.ToObjectFromJson<CreateOrUpdateApiRequest>()!;
        return new CreateOrUpdateApiRequest
        {
            Type = properties.Type,
            ApiRevision = properties.ApiRevision,
            ApiRevisionDescription = properties.ApiRevisionDescription,
            ApiVersion = properties.ApiVersion,
            ApiVersionDescription = properties.ApiVersionDescription,
            ApiVersionSetId = properties.ApiVersionSetId,
            AuthenticationSettings = properties.AuthenticationSettings,
            Contact = properties.Contact,
            Description = properties.Description,
            IsCurrent = properties.IsCurrent,
            Properties = properties.Properties,
            SubscriptionRequired = properties.SubscriptionRequired,
            TermsOfServiceUrl = properties.TermsOfServiceUrl,
            SubscriptionKeyParameterNames = properties.SubscriptionKeyParameterNames,
            License = properties.License
        };
    }
}

[UsedImplicitly]
internal sealed class CreateOrUpdateApiRequestProperties
{
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
}