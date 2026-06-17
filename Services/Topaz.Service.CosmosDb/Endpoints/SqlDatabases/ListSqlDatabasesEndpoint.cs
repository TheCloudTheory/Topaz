using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.CosmosDb.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.CosmosDb.Endpoints.SqlDatabases;

internal sealed class ListSqlDatabasesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly CosmosDbServiceControlPlane _controlPlane = CosmosDbServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.DocumentDB";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}/sqlDatabases"
    ];

    public string[] Permissions => ["Microsoft.DocumentDB/databaseAccounts/sqlDatabases/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var accountName = context.Request.Path.Value.ExtractValueFromPath(8);

        var operation = _controlPlane.ListSqlDatabases(
            subscriptionIdentifier, resourceGroupIdentifier, accountName!);

        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var listResponse = new SqlDatabaseListResponse { Value = operation.Resource ?? [] };
        response.CreateJsonContentResponse(listResponse);
    }
}
