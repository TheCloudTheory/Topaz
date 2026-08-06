using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerInstances.Endpoints;

internal sealed class DeleteContainerGroupEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ContainerInstancesServiceControlPlane _controlPlane =
        ContainerInstancesServiceControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.ContainerInstances";

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerInstance/containerGroups/{containerGroupName}"
    ];

    public string[] Permissions => ["Microsoft.ContainerInstances/service/delete"];

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
            response.CreateNotFoundResponse(existing);
            return;
        }

        var result = _controlPlane.Delete(sub, rg, name);
        if (result.Result == OperationResult.NotFound)
        {
            response.CreateNotFoundResponse(result);
            return;
        }
        
        if (result.Result != OperationResult.Deleted)
        {
            response.CreateErrorResponse(result);
            return;
        }

        response.CreateJsonContentResponse(result.Resource!);
    }
}
