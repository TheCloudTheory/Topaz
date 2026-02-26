using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceGroup.Endpoints;

public class CreateUpdateResourceGroupEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ResourceGroupControlPlane _controlPlane = ResourceGroupControlPlane.New(eventPipeline, logger);
    
    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}",
    ];

    public string[] Permissions => ["Microsoft.Resources/subscriptions/resourceGroups/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        using var reader = new StreamReader(context.Request.Body);
        
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateOrUpdateResourceGroupRequest>(content, GlobalSettings.JsonOptions);
        
        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        var operation = _controlPlane.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, request);
        
        switch (operation.Result)
        {
            case OperationResult.Failed:
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.Content = new StringContent(operation.ToString());
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            
                return;
            case OperationResult.NotFound:
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(operation.ToString());
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            
                return;
            case OperationResult.Created:
            case OperationResult.Updated:
            case OperationResult.Success:
            case OperationResult.Deleted:
            default:
                response.StatusCode = operation.Result == OperationResult.Created ? HttpStatusCode.Created : HttpStatusCode.OK;
                response.Content = new StringContent(operation.Resource!.ToString()!);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                break;
        }
    }
}