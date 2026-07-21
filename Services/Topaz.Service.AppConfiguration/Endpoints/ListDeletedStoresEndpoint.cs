using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.AppConfiguration.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.AppConfiguration.Endpoints;

internal sealed class ListDeletedStoresEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly AppConfigurationServiceControlPlane _controlPlane =
        AppConfigurationServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.AppConfiguration";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.AppConfiguration/locations/{location}/deletedConfigurationStores"
    ];

    public string[] Permissions => ["Microsoft.AppConfiguration/locations/deletedConfigurationStores/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        
        var deleted = _controlPlane.ListDeleted(subscriptionIdentifier);
        if (deleted.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.CreateJsonContentResponse(ListDeletedStoresResponse.From(deleted.Resource));
    }
}