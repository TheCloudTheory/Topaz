using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Sql.Models;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Sql.Endpoints;

internal sealed class GetDatabaseBackupShortTermRetentionPolicyEndpoint : IEndpointDefinition
{
    public string? ProviderNamespace => "Microsoft.Sql";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Sql/servers/{serverName}/databases/{databaseName}/backupShortTermRetentionPolicies/{policyName}"
    ];

    public string[] Permissions => ["Microsoft.Sql/servers/databases/backupShortTermRetentionPolicies/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionId = context.Request.Path.Value!.ExtractValueFromPath(2);
        var resourceGroupName = context.Request.Path.Value!.ExtractValueFromPath(4);
        var serverName = context.Request.Path.Value!.ExtractValueFromPath(8);
        var databaseName = context.Request.Path.Value!.ExtractValueFromPath(10);
        var policyName = context.Request.Path.Value!.ExtractValueFromPath(12);

        var result = BackupShortTermRetentionPolicyResponse.ForDatabase(
            subscriptionId ?? string.Empty,
            resourceGroupName ?? string.Empty,
            serverName ?? string.Empty,
            databaseName ?? string.Empty,
            policyName ?? string.Empty);

        response.CreateJsonContentResponse(result);
        response.StatusCode = HttpStatusCode.OK;
    }
}
