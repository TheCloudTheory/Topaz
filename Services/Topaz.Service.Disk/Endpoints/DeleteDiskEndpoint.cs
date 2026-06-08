using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Disk.Endpoints;

internal sealed class DeleteDiskEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly DiskServiceControlPlane _controlPlane =
        DiskServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Compute";

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Compute/disks/{diskName}"
    ];

    public string[] Permissions => ["Microsoft.Compute/disks/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(DeleteDiskEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier =
                SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier =
                ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var diskName = context.Request.Path.Value.ExtractValueFromPath(8);

            if (string.IsNullOrWhiteSpace(diskName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var existing = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, diskName);

            switch (existing.Result)
            {
                case OperationResult.NotFound:
                    response.StatusCode = HttpStatusCode.NotFound;
                    return;
                case OperationResult.Failed:
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return;
                default:
                    _controlPlane.Delete(subscriptionIdentifier, resourceGroupIdentifier, diskName);
                    response.StatusCode = HttpStatusCode.NoContent;
                    response.Content = new ByteArrayContent([]);
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
