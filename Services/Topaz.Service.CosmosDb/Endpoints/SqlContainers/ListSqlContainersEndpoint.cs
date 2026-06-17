using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.CosmosDb.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.CosmosDb.Endpoints.SqlContainers;

internal sealed class ListSqlContainersEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly CosmosDbServiceControlPlane _controlPlane = CosmosDbServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.DocumentDB";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}/sqlDatabases/{databaseName}/containers"
    ];

    public string[] Permissions => ["Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var accountName = context.Request.Path.Value.ExtractValueFromPath(8);
        var databaseName = context.Request.Path.Value.ExtractValueFromPath(10);

        var operation = _controlPlane.ListSqlContainers(
            subscriptionIdentifier, resourceGroupIdentifier, accountName!, databaseName!);

        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var listResponse = new SqlContainerListResponse { Value = operation.Resource ?? [] };
        response.CreateJsonContentResponse(listResponse);
    }
}
