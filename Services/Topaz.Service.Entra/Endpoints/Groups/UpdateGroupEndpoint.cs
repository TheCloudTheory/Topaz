using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Entra.Endpoints.Groups;

internal sealed class UpdateGroupEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly GroupDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);

    public string[] Endpoints =>
    [
        "PATCH /groups/{groupId}",
        "PATCH /v1.0/groups/{groupId}",
        "PATCH /beta/groups/{groupId}",
    ];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var groupIdentifier = context.Request.Path.Value.StartsWith("/groups")
            ? GroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2))
            : GroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(3));

        logger.LogDebug(nameof(UpdateGroupEndpoint), nameof(GetResponse),
            "Updating group `{0}`.", groupIdentifier);

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<UpdateGroupRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var operation = _dataPlane.Update(groupIdentifier, request);
        if (operation.Result != OperationResult.Updated)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                "Unknown error when performing UpdateGroup operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.NoContent;
    }
}
