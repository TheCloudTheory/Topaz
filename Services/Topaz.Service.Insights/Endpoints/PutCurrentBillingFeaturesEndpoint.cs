using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Insights.Endpoints;

internal sealed class PutCurrentBillingFeaturesEndpoint : IEndpointDefinition
{
    public string? ProviderNamespace => "microsoft.insights";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/microsoft.insights/components/{componentName}/currentbillingfeatures"
    ];

    public string[] Permissions => ["microsoft.insights/components/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        // Reflect the request body back — the azurerm provider sends the cap settings and
        // validates they appear in the response. A verbatim echo satisfies the contract.
        using var reader = new System.IO.StreamReader(context.Request.Body);
        var body = reader.ReadToEnd();
        response.CreateJsonContentResponse(body);
    }
}
