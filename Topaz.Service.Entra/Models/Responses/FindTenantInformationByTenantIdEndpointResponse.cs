using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models.Responses;

public sealed class FindTenantInformationByTenantIdEndpointResponse
{
    [UsedImplicitly] public string? ODataType { get; set; } = "#microsoft.graph.tenantInformation";
    public string DefaultDomainName => EntraService.DefaultDomainName;
    public string DisplayName => EntraService.TenantDisplayName;
    public string FederationBrandName => EntraService.FederationBrandName;
    public string TenantId => EntraService.TenantId;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}