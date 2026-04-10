using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.EventHub.Models;
using Topaz.Service.EventHub.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.EventHub.Endpoints;

public sealed class CreateUpdateNamespaceNetworkRuleSetEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly EventHubServiceControlPlane _controlPlane = new(new EventHubResourceProvider(logger), logger);

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventHub/namespaces/{namespaceName}/networkRuleSets/{networkRuleSetName}"
    ];

    public string[] Permissions => ["Microsoft.EventHub/namespaces/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        using var reader = new StreamReader(context.Request.Body);

        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var namespaceIdentifier = EventHubNamespaceIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(8));
        var networkRuleSetName = context.Request.Path.Value.ExtractValueFromPath(10);

        var namespaceOperation = _controlPlane.GetNamespace(subscriptionIdentifier, resourceGroupIdentifier,
            namespaceIdentifier);
        if (namespaceOperation.Result == OperationResult.NotFound || namespaceOperation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var content = reader.ReadToEnd();
        var request =
            JsonSerializer.Deserialize<CreateOrUpdateNamespaceNetworkRuleSetRequest>(content, GlobalSettings.JsonOptions);
        var properties = request == null
            ? EventHubNetworkRuleSetSubresourceProperties.Default()
            : EventHubNetworkRuleSetSubresourceProperties.From(request);

        var operation = _controlPlane.CreateOrUpdateNetworkRuleSet(subscriptionIdentifier, resourceGroupIdentifier,
            namespaceIdentifier, networkRuleSetName!, properties);
        if ((operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated) ||
            operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                "Unknown error when performing CreateOrUpdate operation.");
            return;
        }

        response.CreateJsonContentResponse(operation.Resource, HttpStatusCode.OK);
    }
}