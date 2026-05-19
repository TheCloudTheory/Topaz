using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.VirtualMachine.Endpoints;

internal sealed class ListVirtualMachineImageVersionsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private const string StubVersion = "22.04.202208100";

    public string? ProviderNamespace => "Microsoft.Compute";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Compute/locations/{location}/publishers/{publisher}/artifacttypes/vmimage/offers/{offer}/skus/{sku}/versions"
    ];

    public string[] Permissions => ["Microsoft.Compute/locations/publishers/artifacttypes/vmimages/skus/versions/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListVirtualMachineImageVersionsEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionId = context.Request.Path.Value.ExtractValueFromPath(2);
            var location = context.Request.Path.Value.ExtractValueFromPath(6);
            var publisher = context.Request.Path.Value.ExtractValueFromPath(8);
            var offer = context.Request.Path.Value.ExtractValueFromPath(12);
            var sku = context.Request.Path.Value.ExtractValueFromPath(14);

            var versionId =
                $"/Subscriptions/{subscriptionId}/Providers/Microsoft.Compute/Locations/{location}" +
                $"/Publishers/{publisher}/ArtifactTypes/VMImage/Offers/{offer}/Skus/{sku}/Versions/{StubVersion}";

            var result = new VmImageVersionListResponse(
            [
                new VmImageVersionEntry { Id = versionId, Location = location!, Name = StubVersion }
            ]);

            response.CreateJsonContentResponse(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }

    private sealed class VmImageVersionEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private sealed class VmImageVersionListResponse(VmImageVersionEntry[] versions)
    {
        public override string ToString() => JsonSerializer.Serialize(versions, GlobalSettings.JsonOptions);
    }
}
