using System.Net;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ServiceBus.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints.DataPlane;

internal sealed class CreateOrUpdateSubscriptionDataPlaneEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ServiceBus";
    public string[] Endpoints => ["PUT /{entity}/Subscriptions/{subscription}"];
    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/topics/subscriptions/write"];

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

        var topicName = context.Request.Path.Value.ExtractValueFromPath(1);
        var subscriptionName = context.Request.Path.Value.ExtractValueFromPath(3);
        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();

        var serializer = new XmlSerializer(typeof(CreateOrUpdateServiceBusSubscriptionAtomRequest));
        if (serializer.Deserialize(new StringReader(content)) is not CreateOrUpdateServiceBusSubscriptionAtomRequest atomRequest)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var operation = _controlPlane.CreateOrUpdateSubscription(subscriptionId!, resourceGroupId!, namespaceName, subscriptionName!, CreateOrUpdateServiceBusSubscriptionRequest.From(atomRequest), topicName!);
        if (operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated || operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode, "Unknown error when performing CreateOrUpdate operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource.ToXmlString());
    }
}
