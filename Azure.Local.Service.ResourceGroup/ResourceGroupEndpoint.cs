using System.Net.Http.Headers;
using System.Net.Http.Json;
using Azure.Local.Service.Shared;
using Azure.Local.Shared;
using Microsoft.AspNetCore.Http;

namespace Azure.Local.Service.ResourceGroup;

public class ResourceGroupEndpoint(ILogger logger) : IEndpointDefinition
{
    private readonly ILogger logger = logger;

    public Protocol Protocol => Protocol.Http;

    public string[] Endpoints => ["/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}"];

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query)
    {
        this.logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");

        var response = new HttpResponseMessage();

        try
        {
            if(method == "PUT")
            {
                var resourceGroupName = ExtractResourceGroupNameFromPath(path);
                var rp = new ResourceProvider(this.logger);

                var (data, code) = rp.CreateOrUpdate(resourceGroupName, input);

                response.StatusCode = code;
                response.Content = JsonContent.Create(data, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
            }

            if (method == "GET")
            {
                var resourceGroupName = ExtractResourceGroupNameFromPath(path);
                var rp = new ResourceProvider(this.logger);

                var (data, code) = rp.Get(resourceGroupName);

                response.StatusCode = code;
                response.Content = JsonContent.Create(data, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
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
