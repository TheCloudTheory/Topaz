using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceGroup.Models.Requests;
using Topaz.Service.ResourceGroup.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup;

public class ResourceGroupEndpoint(ResourceProvider provider, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ResourceGroupControlPlane _controlPlane = new(provider, logger);
    public (int Port, Protocol Protocol) PortAndProtocol => (GlobalSettings.DefaultResourceManagerPort, Protocol.Https);

    public string[] Endpoints => [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}",
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups"
    ];

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query, GlobalOptions options)
    {
        logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");

        var response = new HttpResponseMessage();
        
        var subscriptionId = path.ExtractValueFromPath(2);
        var resourceGroupName = path.ExtractValueFromPath(4);

        try
        {
            switch (method)
            {
                case "PUT":
                {
                    if (string.IsNullOrEmpty(subscriptionId) || string.IsNullOrEmpty(resourceGroupName))
                    {
                        response.StatusCode = HttpStatusCode.BadRequest;
                        break;
                    }
                    
                    HandleCreateOrUpdateResourceGroup(subscriptionId, resourceGroupName, input, response);
                    break;
                }
                case "GET":
                {
                    if (string.IsNullOrEmpty(subscriptionId))
                    {
                        response.StatusCode = HttpStatusCode.BadRequest;
                        break;
                    }

                    if (string.IsNullOrEmpty(resourceGroupName))
                    {
                        HandleListResourceGroup(subscriptionId, response);
                    }
                    else
                    {
                        HandleGetResourceGroup(resourceGroupName, response);
                    }
                    
                    break;
                }
                case "DELETE":
                    if (string.IsNullOrEmpty(resourceGroupName))
                    {
                        response.StatusCode = HttpStatusCode.BadRequest;
                        break;
                    }
                    
                    HandleDeleteResourceGroup(resourceGroupName, response);
                    break;
            }
        }
        catch(Exception ex)
        {
            logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;

            return response;
        }

        return response;
    }

    private void HandleDeleteResourceGroup(string resourceGroupName, HttpResponseMessage response)
    {
        var existingResourceGroup = _controlPlane.Get(resourceGroupName);
        if (existingResourceGroup.result == OperationResult.NotFound)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceGroupNotFoundCode, resourceGroupName);
            return;
        }

        _controlPlane.Delete(resourceGroupName);
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleListResourceGroup(string subscriptionId, HttpResponseMessage response)
    {
        var operation = _controlPlane.List(subscriptionId);
        if (operation.result == OperationResult.Failed)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content =  new StringContent(new ListResourceGroupsResponse(operation.resources).ToString());
    }

    private void HandleGetResourceGroup(string resourceGroupName, HttpResponseMessage response)
    {
        var operation = _controlPlane.Get(resourceGroupName!);
        if (operation.result == OperationResult.NotFound || operation.resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(operation.resource.ToString());
    }

    private void HandleCreateOrUpdateResourceGroup(string subscriptionId, string resourceGroupName, Stream input, HttpResponseMessage response)
    {
        using var reader = new StreamReader(input);
        
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateOrUpdateResourceGroupRequest>(content, GlobalSettings.JsonOptions);
        
        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        var operation = _controlPlane.CreateOrUpdate(resourceGroupName, subscriptionId, request);
        if (operation.result == OperationResult.Failed)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        response.StatusCode = operation.result == OperationResult.Created ? HttpStatusCode.Created : HttpStatusCode.OK;
        response.Content = new StringContent(operation.resource.ToString());
    }
}
