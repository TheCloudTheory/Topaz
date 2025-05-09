using System.Net.Http.Headers;
using System.Net.Http.Json;
using Azure.Local.Service.Shared;
using Azure.Local.Shared;
using Microsoft.AspNetCore.Http;

namespace Azure.Local.Service.Subscription;

public sealed class SubscriptionEndpoint(ResourceProvider provider, ILogger logger) : IEndpointDefinition
{
    private readonly ILogger logger = logger;
    private readonly SubscriptionControlPlane controlPlane = new(provider);

    public Protocol Protocol => Protocol.Https;

    public string[] Endpoints => ["/subscriptions/{subscriptionId}"];

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query)
    {
        this.logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");

        var response = new HttpResponseMessage();

        try
        {
            if(method == "GET")
            {
                var requestParts = path.Split('/');
                var subscriptionId = requestParts[2];
                var subscription = this.controlPlane.Get(subscriptionId);

                response.Content = JsonContent.Create(subscription, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
                response.StatusCode = System.Net.HttpStatusCode.OK;
            }
            else
            {
                response.StatusCode = System.Net.HttpStatusCode.NotFound;
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
