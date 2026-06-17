using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.CosmosDb.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.CosmosDb.Endpoints.SqlDatabases;

internal sealed class CreateOrUpdateSqlDatabaseEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly CosmosDbServiceControlPlane _controlPlane = CosmosDbServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.DocumentDB";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}/sqlDatabases/{databaseName}"
    ];

    public string[] Permissions => ["Microsoft.DocumentDB/databaseAccounts/sqlDatabases/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var accountName = context.Request.Path.Value.ExtractValueFromPath(8);
        var databaseName = context.Request.Path.Value.ExtractValueFromPath(10);

        if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(databaseName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        logger.LogDebug(nameof(CreateOrUpdateSqlDatabaseEndpoint), nameof(GetResponse),
            "Processing payload: {0}", content);

        var request = JsonSerializer.Deserialize<CreateOrUpdateSqlDatabaseRequest>(content, GlobalSettings.JsonOptions);
        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var operation = _controlPlane.CreateOrUpdateSqlDatabase(
            subscriptionIdentifier, resourceGroupIdentifier, accountName!, databaseName!, request);

        if (operation.Resource == null)
        {
            response.CreateErrorResponse(operation.Code ?? HttpResponseMessageExtensions.InternalErrorCode,
                operation.Reason ?? "Unknown error.");
            response.StatusCode = operation.Result == OperationResult.NotFound
                ? HttpStatusCode.NotFound
                : HttpStatusCode.InternalServerError;
            return;
        }

        response.CreateJsonContentResponse(operation.Resource, HttpStatusCode.OK);
    }
}
