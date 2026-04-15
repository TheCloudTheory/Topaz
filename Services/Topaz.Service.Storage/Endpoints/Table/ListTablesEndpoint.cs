using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Table;

internal sealed class ListTablesEndpoint(ITopazLogger logger)
    : TableDataPlaneEndpointBase(logger), IEndpointDefinition
{
    public string[] Endpoints => ["GET /Tables"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/tableServices/tables/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultTableStoragePort], Protocol.Http);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();

        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name,
                context.Request.Headers, context.Request.Method, context.Request.Path, context.Request.QueryString))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            return;
        }

        var tables = ControlPlane.GetTables(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name);
        response.Content = JsonContent.Create(new TableEndpointResponse(tables));
    }

    private sealed class TableEndpointResponse(TableProperties[] tables)
    {
        public TableProperties[] Value { get; init; } = tables;
    }
}
