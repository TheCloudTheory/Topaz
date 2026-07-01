using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.AppConfiguration.Endpoints;

internal sealed class ListConfigurationStoreReplicasEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly AppConfigurationServiceControlPlane _controlPlane =
        AppConfigurationServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.AppConfiguration";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.AppConfiguration/configurationStores/{configStoreName}/replicas"
    ];

    public string[] Permissions => ["Microsoft.AppConfiguration/configurationStores/replicas/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var name = context.Request.Path.Value.ExtractValueFromPath(8);

        var store = _controlPlane.Get(sub, rg, name!);
        if (store.Result == OperationResult.NotFound)
        {
            response.CreateErrorResponse(store.Code!, store.Reason!, HttpStatusCode.NotFound);
            return;
        }

        // Topaz does not emulate replicas — return an empty list.
        response.CreateJsonContentResponse(new EmptyListResponse());
    }
}

internal sealed class GetDeletedConfigurationStoreEndpoint : IEndpointDefinition
{
    public string? ProviderNamespace => "Microsoft.AppConfiguration";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.AppConfiguration/locations/{location}/deletedConfigurationStores/{configStoreName}"
    ];

    public string[] Permissions => ["Microsoft.AppConfiguration/locations/deletedConfigurationStores/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        // Topaz does not emulate soft-delete — always return 404.
        response.StatusCode = HttpStatusCode.NotFound;
    }
}

internal sealed class EmptyListResponse
{
    public object[] Value { get; set; } = [];

    public override string ToString() =>
        JsonSerializer.Serialize(this, Topaz.Shared.GlobalSettings.JsonOptions);
}
