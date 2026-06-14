using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.LoadBalancer.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.LoadBalancer.Endpoints.LoadBalancers;

internal sealed class ListLoadBalancersBySubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly LoadBalancerControlPlane _controlPlane = LoadBalancerControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.Network";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Network/loadBalancers"
    ];

    public string[] Permissions => ["Microsoft.Network/loadBalancers/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListLoadBalancersBySubscriptionEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value!.ExtractValueFromPath(2));

            var lbs = _controlPlane.ListBySubscription(subscriptionIdentifier);
            if (lbs.Result != OperationResult.Success || lbs.Resource == null)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            }

            var result = new ListSubscriptionResourcesResponse
            {
                Value = lbs.Resource.Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From!).ToArray()
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
