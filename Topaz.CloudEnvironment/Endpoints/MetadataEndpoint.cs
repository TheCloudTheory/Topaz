using Microsoft.AspNetCore.Http;
using Topaz.CloudEnvironment.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.CloudEnvironment.Endpoints;

public sealed class MetadataEndpoint : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "GET /metadata/endpoints"
    ];

    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var metadata = new GetMetadataEndpointResponse();
        response.Content = new StringContent(metadata.ToString());
    }
}