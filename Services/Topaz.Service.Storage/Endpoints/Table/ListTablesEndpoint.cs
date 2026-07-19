using Topaz.EventPipeline;
using System.Net;
using System.Net.Http.Json;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Table;

internal sealed class ListTablesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : TableDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    public string ProviderNamespace => "Microsoft.Storage";

    public string[] Endpoints => ["GET /Tables"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/tableServices/tables/read"];

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount, out var originalStorageAccountName))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount.GetResourceGroup();

        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name, context,
                response))
            return;

        var tablesOp = ControlPlane.GetTables(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name,
            originalStorageAccountName!);
        response.Content = JsonContent.Create(new TableEndpointResponse(tablesOp.Resource!));
    }

    private sealed class TableEndpointResponse(TableProperties[] tables)
    {
        [UsedImplicitly] public TableProperties[] Value { get; init; } = tables;
    }
}
