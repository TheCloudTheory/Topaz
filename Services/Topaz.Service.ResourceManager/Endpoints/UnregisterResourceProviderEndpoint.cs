using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ResourceManager.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class UnregisterResourceProviderEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _subscriptionControlPlane =
        SubscriptionControlPlane.New(eventPipeline, logger);

    private readonly ResourceManagerResourceProvider _resourceProvider = new(logger);

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/providers/{providerNamespace}/unregister"
    ];

    public string[] Permissions => ["*/unregister/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var providerNamespace = path.ExtractValueFromPath(4)!;

        var subscriptionOperation = _subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscriptionOperation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new StringContent(subscriptionOperation.ToString());
            return;
        }

        _resourceProvider.SetProviderRegistrationState(
            subscriptionIdentifier.Value, providerNamespace, ResourceProviderDataResponse.UnregisteredState);

        var data = new ResourceProviderDataResponse(providerNamespace)
        {
            Id = $"/subscriptions/{subscriptionIdentifier}/providers/{providerNamespace}",
            RegistrationState = ResourceProviderDataResponse.UnregisteredState,
            RegistrationPolicy = "RegistrationRequired",
        };

        response.CreateJsonContentResponse(data);
    }
}
