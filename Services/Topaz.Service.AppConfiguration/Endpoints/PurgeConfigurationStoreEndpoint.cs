using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.AppConfiguration.Endpoints;

internal sealed class PurgeConfigurationStoreEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly AppConfigurationServiceControlPlane _controlPlane =
        AppConfigurationServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.AppConfiguration";

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/providers/Microsoft.AppConfiguration/locations/{location}/deletedConfigurationStores/{name}/purge"
    ];

    public string[] Permissions => ["Microsoft.AppConfiguration/locations/deletedConfigurationStores/purge/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var storeName = context.Request.Path.Value.ExtractValueFromPath(8);
        
        var deleted = _controlPlane.GetDeleted(subscriptionIdentifier, storeName!);
        if (deleted.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        var result = _controlPlane.Purge(subscriptionIdentifier, storeName!);
        if(result.Result != OperationResult.Purged)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new ByteArrayContent([]);
    }
}