using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Sql.Models;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Sql.Endpoints;

internal sealed class ListSqlDatabasesByServerEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly SqlServiceControlPlane _controlPlane = SqlServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Sql";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Sql/servers/{serverName}/databases"
    ];

    public string[] Permissions => ["Microsoft.Sql/servers/databases/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListSqlDatabasesByServerEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier =
                SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier =
                ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var serverName = context.Request.Path.Value.ExtractValueFromPath(8);

            var databases = _controlPlane.ListDatabases(
                subscriptionIdentifier, resourceGroupIdentifier, serverName!);

            var result = new ListSqlDatabasesResponse { Value = databases.ToArray() };
            response.CreateJsonContentResponse(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }

    private sealed class ListSqlDatabasesResponse
    {
        public SqlDatabaseResource[] Value { get; init; } = [];
        public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
