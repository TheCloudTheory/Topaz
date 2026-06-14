using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.LoadBalancer.Models;
using Topaz.Service.LoadBalancer.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.LoadBalancer.Endpoints.LoadBalancers;

internal sealed class CreateOrUpdateLoadBalancerEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly LoadBalancerControlPlane _controlPlane = LoadBalancerControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.Network";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/loadBalancers/{loadBalancerName}"
    ];

    public string[] Permissions => ["Microsoft.Network/loadBalancers/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(CreateOrUpdateLoadBalancerEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var loadBalancerName = context.Request.Path.Value.ExtractValueFromPath(8);

            using var reader = new StreamReader(context.Request.Body);
            var content = reader.ReadToEnd();
            var request = JsonSerializer.Deserialize<CreateOrUpdateLoadBalancerRequest>(content, GlobalSettings.JsonOptions);

            if (request == null || string.IsNullOrEmpty(loadBalancerName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var result = _controlPlane.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, loadBalancerName, request);

            if (result.Result == OperationResult.NotFound)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            response.StatusCode = HttpStatusCode.Created;
            response.CreateJsonContentResponse(result.Resource!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
