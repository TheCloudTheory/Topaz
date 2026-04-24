using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints.TenantRelationships;

internal sealed class FindTenantInformationByTenantIdEndpoint() : IEndpointDefinition
{
    public string[] Endpoints => [@"GET /tenantRelationships/^findTenantInformationByTenantId\(tenantId='.*?'\)"];
    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        response.CreateJsonContentResponse(new FindTenantInformationByTenantIdEndpointResponse());
    }
}