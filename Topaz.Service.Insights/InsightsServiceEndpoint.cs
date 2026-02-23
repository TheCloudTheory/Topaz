using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Insights;

internal sealed class InsightsServiceEndpoint : IEndpointDefinition
{
    public string[] Endpoints => [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Insights/eventtypes/management/values"
    ];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query,
        GlobalOptions options)
    {
        var response = new HttpResponseMessage();
        var result = new
        {
            value = Array.Empty<object>()
        };

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(JsonSerializer.Serialize(result, GlobalSettings.JsonOptions));
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        
        return response;
    }
}