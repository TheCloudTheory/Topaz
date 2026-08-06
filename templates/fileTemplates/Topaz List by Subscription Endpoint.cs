using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace ${NAMESPACE};

internal sealed class List${NAME}sEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ${CONTROL_PLANE} _controlPlane =
        ${CONTROL_PLANE}.New(eventPipeline, logger);

    public string ProviderNamespace => "${PROVIDER_NAMESPACE}";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/${PROVIDER_NAMESPACE}/${RESOURCE_TYPE}"
    ];

    public string[] Permissions => ["${PROVIDER_NAMESPACE}/${RESOURCE_TYPE}/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));

        var existing = _controlPlane.List(sub);
        if (existing.Result == OperationResult.NotFound)
        {
            response.CreateNotFoundResponse(existing);
            return;
        }

        response.CreateJsonContentResponse(${LIST_RESULT_TYPE}.From(existing.Resource!));
    }
}
