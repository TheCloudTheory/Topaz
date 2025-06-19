using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Shared;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceGroup.Models.Requests;

namespace Topaz.Service.ResourceGroup;

public class ResourceGroupEndpoint(ResourceProvider provider, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ResourceGroupControlPlane _controlPlane = new(provider, logger);
    public (int Port, Protocol Protocol) PortAndProtocol => (GlobalSettings.DefaultResourceManagerPort, Protocol.Https);

    public string[] Endpoints => [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}"
    ];

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query, GlobalOptions options)
    {
        logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");

        var response = new HttpResponseMessage();

        try
        {
            switch (method)
            {
                case "PUT":
                {
                    HandleCreateOrUpdateResourceGroup(path, input, response);
                    break;
                }
                case "GET":
                {
                    HandleGetResourceGroup(path, response);
                    break;
                }
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

    private void HandleGetResourceGroup(string path, HttpResponseMessage response)
    {
        var resourceGroupName = path.ExtractValueFromPath(4);
        var data = _controlPlane.Get(resourceGroupName!);

        response.StatusCode = HttpStatusCode.OK;
        response.Content = JsonContent.Create(data, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
    }

    private void HandleCreateOrUpdateResourceGroup(string path, Stream input, HttpResponseMessage response)
    {
        var subscriptionId = path.ExtractValueFromPath(2);
        var resourceGroupName = path.ExtractValueFromPath(4);

        if (string.IsNullOrEmpty(subscriptionId) || string.IsNullOrEmpty(resourceGroupName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }
        
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
