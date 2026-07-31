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