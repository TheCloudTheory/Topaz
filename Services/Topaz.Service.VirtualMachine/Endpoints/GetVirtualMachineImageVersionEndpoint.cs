using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.VirtualMachine.Endpoints;

internal sealed class GetVirtualMachineImageVersionEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string? ProviderNamespace => "Microsoft.Compute";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Compute/locations/{location}/publishers/{publisher}/artifacttypes/vmimage/offers/{offer}/skus/{sku}/versions/{version}"
    ];

    public string[] Permissions => ["Microsoft.Compute/locations/publishers/artifacttypes/vmimages/skus/versions/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(GetVirtualMachineImageVersionEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionId = context.Request.Path.Value.ExtractValueFromPath(2);
            var location = context.Request.Path.Value.ExtractValueFromPath(6);
            var publisher = context.Request.Path.Value.ExtractValueFromPath(8);
            var offer = context.Request.Path.Value.ExtractValueFromPath(12);
            var sku = context.Request.Path.Value.ExtractValueFromPath(14);
            var version = context.Request.Path.Value.ExtractValueFromPath(16);

            var versionId =
                $"/Subscriptions/{subscriptionId}/Providers/Microsoft.Compute/Locations/{location}" +
                $"/Publishers/{publisher}/ArtifactTypes/VMImage/Offers/{offer}/Skus/{sku}/Versions/{version}";

            var result = new VmImageVersionResponse
            {
                Id = versionId,
                Location = location!,
                Name = version!,
                Properties = new VmImageVersionProperties()
            };

            response.CreateJsonContentResponse(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }

    private sealed class VmImageVersionProperties
    {
        public OsDiskImage OsDiskImage { get; set; } = new();
        public DataDiskImage[] DataDiskImages { get; set; } = [];
        public AutomaticOsUpgradeProperties AutomaticOsUpgradeProperties { get; set; } = new();
        public string HyperVGeneration { get; set; } = "V2";
        public string ReplicaType { get; set; } = "Unmanaged";
        public object[] Features { get; set; } = [];

        [JsonPropertyName("plan")]
        public object? Plan { get; set; } = null;
    }

    private sealed class OsDiskImage
    {
        public string OperatingSystem { get; set; } = "Linux";
        public int SizeInGb { get; set; } = 30;
    }

    private sealed class DataDiskImage
    {
    }

    private sealed class AutomaticOsUpgradeProperties
    {
        public bool AutomaticOsUpgradeSupported { get; set; } = false;
    }

    private sealed class VmImageVersionResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public VmImageVersionProperties Properties { get; set; } = new();

        public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
