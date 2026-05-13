using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork.Models;

public sealed class IpAddressAvailabilityResult
{
    public bool Available { get; init; }

    public IList<string> AvailableIPAddresses { get; init; } = [];

    public bool IsPlatformReserved { get; init; }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
