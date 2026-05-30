using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.EventHub.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.EventHub.Endpoints;

internal sealed class ListKeysEventHubNamespaceEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly EventHubServiceControlPlane _controlPlane = EventHubServiceControlPlane.New(logger);

    public string? ProviderNamespace => "Microsoft.EventHub";

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventHub/namespaces/{namespaceName}/authorizationRules/{authorizationRuleName}/listKeys",
    ];

    public string[] Permissions => ["Microsoft.EventHub/namespaces/authorizationRules/listKeys/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var namespaceIdentifier = EventHubNamespaceIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(8));
        var authorizationRuleName = context.Request.Path.Value.ExtractValueFromPath(10) ?? string.Empty;

        var operation = _controlPlane.GetNamespace(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var keys = ListKeysEventHubNamespaceResponse.For(namespaceIdentifier.Value, authorizationRuleName);
        response.CreateJsonContentResponse(keys);
    }
}
