using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.CosmosDb.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.CosmosDb.Endpoints.SqlContainers;

internal sealed class UpdateSqlContainerThroughputEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly CosmosDbServiceControlPlane _controlPlane = CosmosDbServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.DocumentDB";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}/sqlDatabases/{databaseName}/containers/{containerName}/throughputSettings/default"
    ];

    public string[] Permissions => ["Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/throughputSettings/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var accountName = context.Request.Path.Value.ExtractValueFromPath(8);
        var databaseName = context.Request.Path.Value.ExtractValueFromPath(10);
        var containerName = context.Request.Path.Value.ExtractValueFromPath(12);

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();

        var request = JsonSerializer.Deserialize<UpdateSqlContainerThroughputRequest>(content, GlobalSettings.JsonOptions);
        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var operation = _controlPlane.UpdateSqlContainerThroughput(
            subscriptionIdentifier, resourceGroupIdentifier, accountName!, databaseName!, containerName!, request);

        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.CreateJsonContentResponse(operation.Resource);
    }
}
