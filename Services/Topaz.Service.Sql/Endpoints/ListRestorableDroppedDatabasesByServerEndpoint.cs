using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Sql.Models;
using Topaz.Shared;

namespace Topaz.Service.Sql.Endpoints;

internal sealed class ListRestorableDroppedDatabasesByServerEndpoint : IEndpointDefinition
{
    public string? ProviderNamespace => "Microsoft.Sql";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Sql/servers/{serverName}/restorableDroppedDatabases"
    ];

    public string[] Permissions => ["Microsoft.Sql/servers/restorableDroppedDatabases/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        response.CreateJsonContentResponse(new ListRestorableDroppedDatabasesResponse());
        response.StatusCode = HttpStatusCode.OK;
    }
}
