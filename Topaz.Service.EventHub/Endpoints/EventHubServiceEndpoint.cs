using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Endpoints;

public sealed class EventHubServiceEndpoint(ILogger logger) : IEndpointDefinition
{
    private readonly ILogger logger = logger;
    private readonly EventHubControlPlane controlPlane = new EventHubControlPlane(new ResourceProvider(logger), logger);
    public string[] Endpoints => [
        "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventHub/namespaces/{namespaceName}",
        "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventHub/namespaces/{namespaceName}/eventhubs/{eventHubName}"
    ];
    public (int Port, Protocol Protocol) PortAndProtocol => (8899, Protocol.Https);
    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query)
    {
        this.logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");

        var response = new HttpResponseMessage();

        try
        {
            switch (method)
            {
                case "GET":
                {
                    HandleGetEventHubNamespaceRequest(path, response);
                    break;
                }
                case "PUT":
                {
                    HandleCreateOrUpdateEventHubRequest(path, response, input);
                    break;
                }
            }
        }
        catch(Exception ex)
        {
            this.logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;

            return response;
        }

        return response;
    }

    private void HandleCreateOrUpdateEventHubRequest(string path, HttpResponseMessage response, Stream input)
    {
        var namespaceName = path.ExtractValueFromPath(8);
        var eventhubName = path.ExtractValueFromPath(10);
                    
        var data = this.controlPlane.CreateUpdateEventHub(namespaceName!, eventhubName!, input);
                    
        response.StatusCode = HttpStatusCode.OK;
        response.Content = JsonContent.Create(data, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
    }

    private void HandleGetEventHubNamespaceRequest(string path, HttpResponseMessage response)
    {
        var namespaceName = path.ExtractValueFromPath(8);
        var data = this.controlPlane.GetNamespace(namespaceName!);

        response.StatusCode = HttpStatusCode.OK;
        response.Content = JsonContent.Create(data, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
    }
}