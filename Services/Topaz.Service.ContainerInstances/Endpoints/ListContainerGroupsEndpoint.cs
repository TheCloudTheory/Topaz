using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ContainerInstances.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerInstances.Endpoints;

internal sealed class ListContainerGroupsEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ContainerInstancesServiceControlPlane _controlPlane =
        ContainerInstancesServiceControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.ContainerInstances";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.ContainerInstance/containerGroups"
    ];

    public string[] Permissions => ["Microsoft.ContainerInstances/service/read"];

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

        response.CreateJsonContentResponse(ContainerGroupListResultResponse.From(existing.Resource!));
    }
}
