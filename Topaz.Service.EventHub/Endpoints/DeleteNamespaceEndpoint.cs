using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.EventHub.Endpoints;

public class DeleteNamespaceEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly EventHubServiceControlPlane _controlPlane = new(new EventHubResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventHub/namespaces/{namespaceName}",
    ];

    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var namespaceIdentifier = EventHubNamespaceIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(8));
        var existingNamespace =
            _controlPlane.GetNamespace(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
        
        switch (existingNamespace.Result)
        {
            case OperationResult.NotFound:
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            case OperationResult.Failed:
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            default:
                _controlPlane.DeleteNamespace(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
                response.StatusCode = HttpStatusCode.OK;
                break;
        }
    }
}