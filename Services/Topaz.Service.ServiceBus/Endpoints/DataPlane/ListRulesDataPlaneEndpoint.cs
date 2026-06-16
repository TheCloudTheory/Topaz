using System.Net;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints.DataPlane;

internal sealed class ListRulesDataPlaneEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";

    private readonly ServiceBusServiceControlPlane _controlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ServiceBus";
    public string[] Endpoints => ["GET /{entity}/Subscriptions/{subscription}/Rules"];
    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/topics/subscriptions/rules/read"];

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

        var operation = _controlPlane.ListRules(subscriptionId!, resourceGroupId!, namespaceName, topicName, subscriptionName);
        if (operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var feed = new XElement(Atom + "feed",
            operation.Resource.Select(r => r.ToEntryElement()));

        response.Content = new StringContent(feed.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }
}
