using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints.DataPlane;

internal sealed class DeleteRuleDataPlaneEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ServiceBus";
    public string[] Endpoints => ["DELETE /{entity}/Subscriptions/{subscription}/Rules/{ruleName}"];
    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/topics/subscriptions/rules/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.AdditionalServiceBusPort, GlobalSettings.HttpsPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var namespaceName = ServiceBusNamespaceIdentifier.From(context.Request.Headers["Host"].ToString().Split(".")[0]);
        var (result, subscriptionId, resourceGroupId) = ServiceBusServiceControlPlane.GetIdentifiersForParentResource(namespaceName);
        if (result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var topicName = context.Request.Path.Value.ExtractValueFromPath(1)!;
        var subscriptionName = context.Request.Path.Value.ExtractValueFromPath(3)!;
        var ruleName = context.Request.Path.Value.ExtractValueFromPath(5)!;

        var deleteResult = _controlPlane.DeleteRule(subscriptionId!, resourceGroupId!, namespaceName, topicName, subscriptionName, ruleName);
        if (deleteResult == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
    }
}
