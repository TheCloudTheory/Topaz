using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace ${NAMESPACE};

internal sealed class Get${NAME}Endpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ${CONTROL_PLANE} _controlPlane =
        ${CONTROL_PLANE}.New(eventPipeline, logger);

    public string ProviderNamespace => "${PROVIDER_NAMESPACE}";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/${PROVIDER_NAMESPACE}/${RESOURCE_TYPE}/{name}"
    ];

    public string[] Permissions => ["${PROVIDER_NAMESPACE}/${RESOURCE_TYPE}/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var name = context.Request.Path.Value.ExtractValueFromPath(${PATH_INDEX});

        if (string.IsNullOrWhiteSpace(name))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var existing = _controlPlane.Get(sub, rg, name);
        if (existing.Result == OperationResult.NotFound)
        {
            response.CreateNotFoundResponse(existing);
            return;
        }

        response.CreateJsonContentResponse(existing.Resource!);
    }
}
