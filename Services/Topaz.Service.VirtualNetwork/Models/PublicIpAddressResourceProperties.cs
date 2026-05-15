using JetBrains.Annotations;
using Topaz.Service.VirtualNetwork.Models.Requests;

namespace Topaz.Service.VirtualNetwork.Models;

public sealed class PublicIpAddressResourceProperties
{
    public string PublicIPAllocationMethod { get; set; } = "Dynamic";
    public string PublicIPAddressVersion { get; set; } = "IPv4";
    public int IdleTimeoutInMinutes { get; set; } = 4;
    public string IpAddress { get; set; } = string.Empty;
    [UsedImplicitly] public string ProvisioningState => "Succeeded";

    internal static PublicIpAddressResourceProperties FromRequest(CreateOrUpdatePublicIpAddressRequest request)
    {
        return new PublicIpAddressResourceProperties
        {
            PublicIPAllocationMethod = request.Properties?.PublicIPAllocationMethod ?? "Dynamic",
            PublicIPAddressVersion = request.Properties?.PublicIPAddressVersion ?? "IPv4",
            IdleTimeoutInMinutes = request.Properties?.IdleTimeoutInMinutes ?? 4,
            IpAddress = GenerateStubIpAddress()
        };
    }

    internal static void UpdateFromRequest(
        PublicIpAddressResourceProperties properties,
        CreateOrUpdatePublicIpAddressRequest request)
    {
        properties.PublicIPAllocationMethod = request.Properties?.PublicIPAllocationMethod ?? properties.PublicIPAllocationMethod;
        properties.PublicIPAddressVersion = request.Properties?.PublicIPAddressVersion ?? properties.PublicIPAddressVersion;
        properties.IdleTimeoutInMinutes = request.Properties?.IdleTimeoutInMinutes ?? properties.IdleTimeoutInMinutes;
        // IpAddress is assigned once on creation and never changed on update
    }

    private static string GenerateStubIpAddress()
    {
        // Use the RFC 5737 documentation range (203.0.113.0/24) for stub addresses
        var last = Random.Shared.Next(1, 254);
        return $"203.0.113.{last}";
    }
}
