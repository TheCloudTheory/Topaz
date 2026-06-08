using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Disk.Endpoints;

internal sealed class ListDisksBySubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly DiskServiceControlPlane _controlPlane =
        DiskServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Compute";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Compute/disks"
    ];

    public string[] Permissions => ["Microsoft.Compute/disks/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListDisksBySubscriptionEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        var subscriptionIdentifier =
            SubscriptionIdentifier.From(context.Request.Path.Value!.ExtractValueFromPath(2));

        var disks = _controlPlane.ListBySubscription(subscriptionIdentifier);
        if (disks.Result != OperationResult.Success || disks.Resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = new ListSubscriptionResourcesResponse
        {
            Value = disks.Resource
                .Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From!)
                .ToArray()
        };

        response.CreateJsonContentResponse(result);
    }
}
