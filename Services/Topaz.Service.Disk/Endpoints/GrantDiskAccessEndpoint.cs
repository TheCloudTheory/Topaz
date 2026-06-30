using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Disk.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Disk.Endpoints;

internal sealed class GrantDiskAccessEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly DiskServiceControlPlane _controlPlane =
        DiskServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Compute";

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Compute/disks/{diskName}/beginGetAccess"
    ];

    public string[] Permissions => ["Microsoft.Compute/disks/beginGetAccess/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(GrantDiskAccessEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        var subscriptionId = context.Request.Path.Value.ExtractValueFromPath(2);
        var subscriptionIdentifier = SubscriptionIdentifier.From(subscriptionId);
        var resourceGroupIdentifier =
            ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var diskName = context.Request.Path.Value.ExtractValueFromPath(8);

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<GrantAccessRequest>(content, GlobalSettings.JsonOptions)
                      ?? new GrantAccessRequest();

        var result = _controlPlane.GrantAccess(subscriptionIdentifier, resourceGroupIdentifier, diskName!, request);

        if (result.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            return;
        }

        if (result.Result == OperationResult.Conflict)
        {
            response.StatusCode = HttpStatusCode.Conflict;
            response.Content = new ByteArrayContent([]);
            return;
        }

        var (operationId, _) = result.Resource;
        var apiVersion = context.Request.Query.TryGetValue("api-version", out var v) ? v.ToString() : "2023-04-02";
        var pollingUrl =
            $"https://{GlobalSettings.TopazHostname}:{GlobalSettings.DefaultResourceManagerPort}" +
            $"/subscriptions/{subscriptionId}/providers/Microsoft.Compute/locations/eastus/diskOperations/{operationId}" +
            $"?api-version={apiVersion}";

        response.Headers.TryAddWithoutValidation("Azure-AsyncOperation", pollingUrl);
        response.Headers.TryAddWithoutValidation("Location", pollingUrl);
        response.StatusCode = HttpStatusCode.Accepted;
        response.Content = new ByteArrayContent([]);
    }
}
