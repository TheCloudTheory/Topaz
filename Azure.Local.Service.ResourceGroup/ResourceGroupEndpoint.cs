using Azure.Local.Service.Shared;
using Azure.Local.Shared;
using Microsoft.AspNetCore.Http;

namespace Azure.Local.Service.ResourceGroup;

public class ResourceGroupEndpoint(ILogger logger) : IEndpointDefinition
{
    private readonly ILogger logger = logger;

    public Protocol Protocol => Protocol.Http;

    public string DnsName => "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}";

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query)
    {
        this.logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");

        var response = new HttpResponseMessage();

        try
        {
            if(method == "PUT")
            {
                var requestParts = path.Split('/');
                var resourceGroupName = requestParts[4];
                var rp = new ResourceProvider(this.logger);

                rp.CreateOrUpdate(resourceGroupName, input);
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
}
