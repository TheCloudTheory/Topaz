using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Sql.Endpoints;

internal sealed class DeleteSqlDatabaseEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly SqlServiceControlPlane _controlPlane = SqlServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Sql";

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Sql/servers/{serverName}/databases/{databaseName}"
    ];

    public string[] Permissions => ["Microsoft.Sql/servers/databases/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(DeleteSqlDatabaseEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier =
                SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier =
                ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var serverName = context.Request.Path.Value.ExtractValueFromPath(8);
            var databaseName = context.Request.Path.Value.ExtractValueFromPath(10);

            if (string.IsNullOrWhiteSpace(serverName) || string.IsNullOrWhiteSpace(databaseName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var operation = _controlPlane.DeleteDatabase(
                subscriptionIdentifier, resourceGroupIdentifier, serverName, databaseName);

            response.StatusCode = operation.Result switch
            {
                OperationResult.NotFound => HttpStatusCode.NotFound,
                OperationResult.Deleted => HttpStatusCode.OK,
                _ => HttpStatusCode.InternalServerError
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
