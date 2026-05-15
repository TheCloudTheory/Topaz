using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.VirtualNetwork.Endpoints;

internal sealed class ListNetworkInterfacesBySubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly NetworkInterfaceControlPlane _controlPlane = NetworkInterfaceControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.Network";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Network/networkInterfaces"
    ];

    public string[] Permissions => ["Microsoft.Network/networkInterfaces/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListNetworkInterfacesBySubscriptionEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value!.ExtractValueFromPath(2));

            var nics = _controlPlane.ListBySubscription(subscriptionIdentifier);
            if (nics.Result != OperationResult.Success || nics.Resource == null)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            }

            var result = new ListSubscriptionResourcesResponse
            {
                Value = nics.Resource.Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From!).ToArray()
            };

            response.CreateJsonContentResponse(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
