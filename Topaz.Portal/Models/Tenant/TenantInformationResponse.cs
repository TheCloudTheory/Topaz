using System.Text.Json.Serialization;
using Microsoft.Graph.Models;

namespace Topaz.Portal.Models.Tenant;

public sealed class TenantInformationResponse
{
    public string? TenantId { get; init; }
    public string? DisplayName { get; init; }
    public string? DefaultDomainName { get; init; }
    public string? FederationBrandName { get; init; }

    /// <summary>
    /// Captures any extra fields Graph sends that aren’t explicitly modeled above.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object?>? AdditionalData { get; init; }

    public static TenantInformationResponse FromGraph(TenantInformation model)
    {
        return new TenantInformationResponse
        {
            TenantId = model.TenantId,
            DisplayName = model.DisplayName,
            DefaultDomainName = model.DefaultDomainName,
            FederationBrandName = model.FederationBrandName,
            AdditionalData = model.AdditionalData
        };
    }
}