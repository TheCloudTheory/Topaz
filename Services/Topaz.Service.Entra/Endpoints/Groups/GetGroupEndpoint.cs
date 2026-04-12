using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Entra.Endpoints.Groups;

internal sealed class GetGroupEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly GroupDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);

    public string[] Endpoints =>
    [
        "GET /groups/{groupId}",
        "GET /v1.0/groups/{groupId}",
        "GET /beta/groups/{groupId}",
    ];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var groupIdentifier = context.Request.Path.Value.StartsWith("/groups")
            ? GroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2))
            : GroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(3));

        logger.LogDebug(nameof(GetGroupEndpoint), nameof(GetResponse),
            "Fetching group `{0}`.", groupIdentifier);

        var operation = _dataPlane.Get(groupIdentifier);
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
