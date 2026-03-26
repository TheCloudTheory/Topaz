using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Authorization.Endpoints;

public sealed class ResourceGroupAuthorizationEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => [];
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        throw new NotImplementedException();
    }
}