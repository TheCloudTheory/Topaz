using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.VirtualMachine.Models;

internal sealed class ComputeResourceSkuCapability
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    public static ComputeResourceSkuCapability New(string name, string value)
        => new() { Name = name, Value = value };
}

internal sealed class ComputeResourceSkuLocationInfo
{
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("zones")]
    public string[] Zones { get; set; } = [];
}

internal sealed class ComputeResourceSkuEntry
{
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tier")]
    public string Tier { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public string Size { get; set; } = string.Empty;

    [JsonPropertyName("locations")]
    public string[] Locations { get; set; } = [];

    [JsonPropertyName("locationInfo")]
    public ComputeResourceSkuLocationInfo[] LocationInfo { get; set; } = [];

    [JsonPropertyName("capabilities")]
    public ComputeResourceSkuCapability[] Capabilities { get; set; } = [];

    [JsonPropertyName("restrictions")]
    public object[] Restrictions { get; set; } = [];

    public static ComputeResourceSkuEntry ForVirtualMachine(string skuName, string tier, string location, bool supportsPremiumIo)
        => new()
        {
            ResourceType = "virtualMachines",
            Name = skuName,
            Tier = tier,
            Size = skuName,
            Locations = [location],
            LocationInfo = [new ComputeResourceSkuLocationInfo { Location = location, Zones = ["1", "2", "3"] }],
            Capabilities =
            [
                ComputeResourceSkuCapability.New("PremiumIO", supportsPremiumIo ? "True" : "False"),
                ComputeResourceSkuCapability.New("OSVhdSizeMB", "1047552"),
                ComputeResourceSkuCapability.New("MaxDataDiskCount", "4"),
                ComputeResourceSkuCapability.New("MemoryGB", "8"),
                ComputeResourceSkuCapability.New("vCPUs", "2")
            ],
            Restrictions = []
        };
}

internal sealed class ComputeResourceSkuListResponse(ComputeResourceSkuEntry[] value)
{
    [JsonPropertyName("value")]
    public ComputeResourceSkuEntry[] Value { get; } = value;

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
