using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Redis.Endpoints;

internal sealed class ListRedisBySubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly RedisServiceControlPlane _controlPlane =
        RedisServiceControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.Cache";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{sub}/providers/Microsoft.Cache/redis"
    ];

    public string[] Permissions => ["Microsoft.Cache/redis/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value!.ExtractValueFromPath(2));

        var result = _controlPlane.ListBySubscription(sub);
        if (result.Result != OperationResult.Success || result.Resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var list = new ListSubscriptionResourcesResponse
        {
            Value =
            [
                .. result.Resource
                    .Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From)
            ]
        };

        response.CreateJsonContentResponse(list);
    }
}
