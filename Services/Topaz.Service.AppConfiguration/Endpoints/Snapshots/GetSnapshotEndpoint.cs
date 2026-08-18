using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.AppConfiguration.Endpoints.Snapshots;

internal sealed class GetSnapshotEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly  AppConfigurationServiceControlPlane _controlPlane =
        AppConfigurationServiceControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.AppConfiguration";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.AppConfiguration/configurationStores/{configStoreName}/snapshots/{snapshotName}"
    ];

    public string[] Permissions => ["Microsoft.AppConfiguration/configurationStores/snapshots/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var storeName = context.Request.Path.Value.ExtractValueFromPath(8);
        var snapshotName = context.Request.Path.Value.ExtractValueFromPath(10);

        if (string.IsNullOrWhiteSpace(storeName) || string.IsNullOrWhiteSpace(snapshotName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var existing = _controlPlane.GetSnapshot(sub, rg, storeName, snapshotName);
        if (existing.Result == OperationResult.NotFound)
        {
            response.CreateNotFoundResponse(existing);
            return;
        }

        response.CreateJsonContentResponse(existing.Resource!);
    }
}
