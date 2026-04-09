using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Endpoints;

/// <summary>
/// Serves ARM environment metadata for custom-cloud clients (e.g. Terraform azurerm provider
/// with ARM_ENVIRONMENT=custom, ARM_METADATA_HOST=&lt;host&gt;).
/// </summary>
internal sealed class GetMetadataEndpointsEndpoint : IEndpointDefinition
{
    public string[] Endpoints => ["GET /metadata/endpoints"];
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var baseUrl = $"https://{context.Request.Host}/";

        var metadata = $$"""
            {
              "galleryEndpoint": "{{baseUrl}}",
              "graphEndpoint": "{{baseUrl}}",
              "portalEndpoint": "{{baseUrl}}",
              "authentication": {
                "loginEndpoint": "{{baseUrl}}",
                "audiences": [
                  "https://management.core.windows.net/",
                  "https://management.azure.com/"
                ]
              }
            }
            """;

        response.CreateJsonContentResponse(metadata);
    }
}
