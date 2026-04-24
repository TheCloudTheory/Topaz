using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints.Directory;

internal sealed class GetDirectoryEndpoint() : IEndpointDefinition
{
    public string[] Endpoints => ["GET /directory"];
    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        response.CreateJsonContentResponse(new Models.Directory
        {
            Id = EntraService.TenantId
        });
    }
}