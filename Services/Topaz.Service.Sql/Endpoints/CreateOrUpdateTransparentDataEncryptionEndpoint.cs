using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Sql.Models;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Sql.Endpoints;

internal sealed class CreateOrUpdateTransparentDataEncryptionEndpoint : IEndpointDefinition
{
    public string? ProviderNamespace => "Microsoft.Sql";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Sql/servers/{serverName}/databases/{databaseName}/transparentDataEncryption/current"
    ];

    public string[] Permissions => ["Microsoft.Sql/servers/databases/transparentDataEncryption/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionId = context.Request.Path.Value!.ExtractValueFromPath(2);
        var resourceGroupName = context.Request.Path.Value!.ExtractValueFromPath(4);
        var serverName = context.Request.Path.Value!.ExtractValueFromPath(8);
        var databaseName = context.Request.Path.Value!.ExtractValueFromPath(10);

        var result = TransparentDataEncryptionResponse.ForDatabase(
            subscriptionId ?? string.Empty,
            resourceGroupName ?? string.Empty,
            serverName ?? string.Empty,
            databaseName ?? string.Empty);

        response.CreateJsonContentResponse(result);
        response.StatusCode = HttpStatusCode.OK;
    }
}
