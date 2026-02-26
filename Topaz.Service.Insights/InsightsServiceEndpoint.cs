using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Insights;

internal sealed class InsightsServiceEndpoint : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Insights/eventtypes/management/values"
    ];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var result = new
        {
            value = Array.Empty<object>()
        };

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(JsonSerializer.Serialize(result, GlobalSettings.JsonOptions));
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}