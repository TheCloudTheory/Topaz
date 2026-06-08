using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Disk.Endpoints;

internal sealed class RevokeDiskAccessEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly DiskServiceControlPlane _controlPlane =
        DiskServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Compute";

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Compute/disks/{diskName}/endGetAccess"
    ];

    public string[] Permissions => ["Microsoft.Compute/disks/endGetAccess/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(RevokeDiskAccessEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        var subscriptionIdentifier =
            SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier =
            ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var diskName = context.Request.Path.Value.ExtractValueFromPath(8);

        var result = _controlPlane.RevokeAccess(subscriptionIdentifier, resourceGroupIdentifier, diskName!);

        response.StatusCode = result == OperationResult.NotFound
            ? HttpStatusCode.NotFound
            : HttpStatusCode.OK;

        response.Content = new ByteArrayContent([]);
    }
}
