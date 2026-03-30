using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagedIdentity.Endpoints;

internal sealed class GetSystemAssignedIdentityByResourceEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private const string PathSuffix = "/providers/Microsoft.ManagedIdentity/identities/default";

    private readonly SystemAssignedIdentityControlPlane _controlPlane = SystemAssignedIdentityControlPlane.New(logger);

    public string[] Endpoints =>
    [
        "GET /.../providers/Microsoft.ManagedIdentity/identities/default"
    ];

    public string[] Permissions => ["Microsoft.ManagedIdentity/identities/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;

        if (!path.EndsWith(PathSuffix, StringComparison.OrdinalIgnoreCase))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var parentResourceId = path[..^PathSuffix.Length];

        var operation = _controlPlane.Get(parentResourceId);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource.ToString());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
