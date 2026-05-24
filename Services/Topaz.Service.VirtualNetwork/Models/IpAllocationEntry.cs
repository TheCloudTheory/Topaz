using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork.Models;

internal sealed class IpAllocationEntry
{
    public string IpAddress { get; init; } = string.Empty;

    public string ResourceId { get; init; } = string.Empty;

    public string SubnetId { get; init; } = string.Empty;

    public static IpAllocationEntry Create(string ipAddress, string resourceId, string subnetId) =>
        new() { IpAddress = ipAddress, ResourceId = resourceId, SubnetId = subnetId };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
