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

public sealed class ListResourceProvidersEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _subscriptionControlPlane =
        SubscriptionControlPlane.New(eventPipeline, logger);

    private readonly ResourceManagerResourceProvider _resourceProvider = new(logger);
    private readonly ResourceManagerControlPlane _controlPlane = ResourceManagerControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers"
    ];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier =
            SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));

        var subscriptionOperation = _subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscriptionOperation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new StringContent(subscriptionOperation.ToString());
            return;
        }

        var providerNames = _controlPlane.GetAllProviders();

        var providers = providerNames.Resource!
            .Select(providerName => new ResourceProviderDataResponse(providerName)
            {
                Id = $"/subscriptions/{subscriptionIdentifier}/providers/{providerName}",
                RegistrationState = _resourceProvider.GetProviderRegistrationState(
                    subscriptionIdentifier.Value, providerName),
                RegistrationPolicy = "RegistrationRequired",
            })
            .ToArray();

        response.CreateJsonContentResponse(new ListResourceProvidersResponse
        {
            Value = providers
        });
    }
}