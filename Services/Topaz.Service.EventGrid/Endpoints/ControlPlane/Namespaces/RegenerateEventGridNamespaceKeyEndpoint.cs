using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.EventGrid.Models.Requests;
using Topaz.Service.EventGrid.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.EventGrid.Endpoints.ControlPlane.Namespaces;

internal sealed class RegenerateEventGridNamespaceKeyEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly EventGridNamespaceControlPlane _controlPlane =
        EventGridNamespaceControlPlane.New(eventPipeline, logger);
    
    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventGrid/namespaces/{namespaceName}/regenerateKey"
    ];

    public string[] Permissions => ["Microsoft.EventGrid/namespaces/regenerateKey/action"];
    public string ProviderNamespace => "Microsoft.EventGrid";
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var name = context.Request.Path.Value.ExtractValueFromPath(8);

        if (string.IsNullOrWhiteSpace(name))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }
        
        using var reader = new StreamReader(context.Request.Body);
        var request = JsonSerializer.Deserialize<RegenerateNamespaceKeyRequest>(reader.ReadToEnd(), GlobalSettings.JsonOptions);
        if (request == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _controlPlane.RegenerateKey(subscriptionIdentifier, resourceGroupIdentifier, name, request);
        if (result.Result != OperationResult.Success || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }
        
        response.CreateJsonContentResponse(ListNamespaceKeysResponse.From(result.Resource));
    }
}