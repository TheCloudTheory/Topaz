using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Topaz.Service.Shared;
using Topaz.Shared;
using Microsoft.AspNetCore.Http;

namespace Topaz.Service.ResourceGroup;

public class ResourceGroupEndpoint(ResourceProvider provider, ILogger logger) : IEndpointDefinition
{
    private readonly ILogger logger = logger;
    private readonly ResourceGroupControlPlane controlPlane = new(provider);
    public (int Port, Protocol Protocol) PortAndProtocol => (8899, Protocol.Https);

    public string[] Endpoints => [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}"
    ];

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query)
    {
        this.logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");

        var response = new HttpResponseMessage();

        try
        {
            switch (method)
            {
                case "PUT":
                {
                    var subscriptionId = path.ExtractValueFromPath(2);
                    var resourceGroupName = path.ExtractValueFromPath(4);
                    var (data, code) = this.controlPlane.CreateOrUpdate(resourceGroupName!, subscriptionId!, input);

                    response.StatusCode = code;
                    response.Content = JsonContent.Create(data, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
                    break;
                }
                case "GET":
                {
                    var resourceGroupName = path.ExtractValueFromPath(4);
                    var data = this.controlPlane.Get(resourceGroupName!);

                    response.StatusCode = HttpStatusCode.OK;
                    response.Content = JsonContent.Create(data, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
                    break;
                }
            }
        }
        catch(Exception ex)
        {
            this.logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = System.Net.HttpStatusCode.InternalServerError;

            return response;
        }

        return response;
    }

    private static string ExtractResourceGroupNameFromPath(string path)
    {
        var requestParts = path.Split('/');
        var resourceGroupName = requestParts[4];
        return resourceGroupName;
    }
}
