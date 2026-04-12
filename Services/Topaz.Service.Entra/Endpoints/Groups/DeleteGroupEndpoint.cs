using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Entra.Endpoints.Groups;

internal sealed class DeleteGroupEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly GroupDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);

    public string[] Endpoints =>
    [
        "DELETE /groups/{groupId}",
        "DELETE /v1.0/groups/{groupId}",
        "DELETE /beta/groups/{groupId}",
    ];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var groupIdentifier = context.Request.Path.Value.StartsWith("/groups")
            ? GroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2))
            : GroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(3));

        logger.LogDebug(nameof(DeleteGroupEndpoint), nameof(GetResponse),
            "Deleting group `{0}`.", groupIdentifier);

        var operation = _dataPlane.Delete(groupIdentifier);
        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.StatusCode = HttpStatusCode.NoContent;
    }
}
