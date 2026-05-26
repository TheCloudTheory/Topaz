using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Sql.Endpoints;

internal sealed class DeleteSqlServerEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly SqlServiceControlPlane _controlPlane = SqlServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Sql";

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Sql/servers/{serverName}"
    ];

    public string[] Permissions => ["Microsoft.Sql/servers/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(DeleteSqlServerEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier =
                SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier =
                ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var serverName = context.Request.Path.Value.ExtractValueFromPath(8);

            if (string.IsNullOrWhiteSpace(serverName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var existing = _controlPlane.Get(
                subscriptionIdentifier, resourceGroupIdentifier, serverName);

            switch (existing.Result)
            {
                case OperationResult.NotFound:
                    response.StatusCode = HttpStatusCode.NotFound;
                    return;
                case OperationResult.Failed:
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return;
                default:
                    _controlPlane.Delete(subscriptionIdentifier, resourceGroupIdentifier, serverName);
                    response.StatusCode = HttpStatusCode.OK;
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
