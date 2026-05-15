using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.VirtualNetwork.Endpoints.NetworkSecurityGroups;

internal sealed class ListNetworkSecurityGroupsBySubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly NetworkSecurityGroupControlPlane _controlPlane = NetworkSecurityGroupControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.Network";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Network/networkSecurityGroups"
    ];

    public string[] Permissions => ["Microsoft.Network/networkSecurityGroups/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListNetworkSecurityGroupsBySubscriptionEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value!.ExtractValueFromPath(2));

            var nsgs = _controlPlane.ListBySubscription(subscriptionIdentifier);
            if (nsgs.Result != OperationResult.Success || nsgs.Resource == null)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            }

            var result = new ListSubscriptionResourcesResponse
            {
                Value = nsgs.Resource.Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From!).ToArray()
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
