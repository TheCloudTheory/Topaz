using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Insights.Endpoints;

internal sealed class DeleteComponentEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ApplicationInsightsServiceControlPlane _controlPlane =
        ApplicationInsightsServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "microsoft.insights";

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/microsoft.insights/components/{componentName}"
    ];

    public string[] Permissions => ["microsoft.insights/components/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var name = context.Request.Path.Value.ExtractValueFromPath(8);

        if (string.IsNullOrWhiteSpace(name))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var existing = _controlPlane.Get(sub, rg, name);
        if (existing.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        _controlPlane.Delete(sub, rg, name);
        response.StatusCode = HttpStatusCode.NoContent;
        response.Content = new ByteArrayContent([]);
    }
}
