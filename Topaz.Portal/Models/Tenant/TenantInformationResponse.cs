using System.Text.Json.Serialization;
using Microsoft.Graph.Models;

namespace Topaz.Portal.Models.Tenant;

public sealed class TenantInformationResponse
{
    public string? TenantId { get; private init; }
    public string? DisplayName { get; private init; }
    public string? DefaultDomainName { get; private init; }
    public string? FederationBrandName { get; private init; }
    
    public long? UsersCount { get; init; }
    public long? GroupsCount { get; init; }
    public long? ServicePrincipalsCount { get; init; }
    public long? ApplicationsCount { get; init; }

    /// <summary>
    /// Captures any extra fields Graph sends that aren’t explicitly modeled above.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object?>? AdditionalData { get; init; }

    public static TenantInformationResponse FromGraph(
        TenantInformation model,
        long? usersCount,
        long? groupsCount,
        long? servicePrincipalsCount,
        long? applicationsCount)
    {
        return new TenantInformationResponse
        {
            TenantId = model.TenantId,
            DisplayName = model.DisplayName,
            DefaultDomainName = model.DefaultDomainName,
            FederationBrandName = model.FederationBrandName,
            UsersCount = usersCount,
            GroupsCount = groupsCount,
            ServicePrincipalsCount = servicePrincipalsCount,
            ApplicationsCount = applicationsCount,
            AdditionalData = model.AdditionalData
        };
    }
}