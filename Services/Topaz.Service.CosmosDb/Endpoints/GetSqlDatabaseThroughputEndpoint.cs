using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.CosmosDb.Endpoints;

internal sealed class GetSqlDatabaseThroughputEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly CosmosDbServiceControlPlane _controlPlane = CosmosDbServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.DocumentDB";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}/sqlDatabases/{databaseName}/throughputSettings/default"
    ];

    public string[] Permissions => ["Microsoft.DocumentDB/databaseAccounts/sqlDatabases/throughputSettings/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var accountName = context.Request.Path.Value.ExtractValueFromPath(8);
        var databaseName = context.Request.Path.Value.ExtractValueFromPath(10);

        var operation = _controlPlane.GetSqlDatabaseThroughput(
            subscriptionIdentifier, resourceGroupIdentifier, accountName!, databaseName!);

        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.CreateJsonContentResponse(operation.Resource);
    }
}
