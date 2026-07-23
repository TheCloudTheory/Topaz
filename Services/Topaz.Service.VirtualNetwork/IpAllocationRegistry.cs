using System.Net;
using Topaz.Service.Shared.Domain;
using Topaz.Service.VirtualNetwork.Models;

namespace Topaz.Service.VirtualNetwork;

internal sealed class IpAllocationRegistry(VirtualNetworkResourceProvider provider)
{
    private const string SubresourceName = "ipallocations";

    // Azure reserves the first 4 addresses in every subnet:
    // .0 (network address), .1 (gateway), .2 (Azure DNS), .3 (future reserved)
    // and the last address (.N broadcast). Start allocation from offset 4.
    private const uint AzureReservedLeadingCount = 4;

    public void Register(string subnetId, string ipAddress, string resourceId)
    {
        var (subscriptionIdentifier, resourceGroupIdentifier, vnetName) = ParseSubnetId(subnetId);
        if (vnetName == null) return;

        var entry = IpAllocationEntry.Create(ipAddress, resourceId, subnetId);
        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, ipAddress, vnetName, SubresourceName, entry);
    }

    public void Unregister(string subnetId, string ipAddress)
    {
        var (subscriptionIdentifier, resourceGroupIdentifier, vnetName) = ParseSubnetId(subnetId);
        if (vnetName == null) return;

        provider.DeleteSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, ipAddress, vnetName, SubresourceName);
    }

    public bool IsAllocated(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vnetName,
        string ipAddress)
    {
        var entry = provider.GetSubresourceAs<IpAllocationEntry>(
            subscriptionIdentifier, resourceGroupIdentifier, ipAddress, vnetName, SubresourceName);
        return entry != null;
    }

    public IReadOnlyList<string> GetAvailableIps(
        string subnetCidr,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vnetName,
        int count = 5)
    {
        if (!IPNetwork.TryParse(subnetCidr, out var network))
            return [];

        var allocated = provider
            .ListSubresourcesAs<IpAllocationEntry>(subscriptionIdentifier, resourceGroupIdentifier, vnetName, SubresourceName)
            .Select(e => e.IpAddress)
            .Where(ip => !string.IsNullOrEmpty(ip))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = new List<string>(count);
        var baseInt = ToUInt32(network.BaseAddress.GetAddressBytes());
        var hostBits = 32 - network.PrefixLength;
        var totalAddresses = 1u << hostBits;

        for (uint offset = AzureReservedLeadingCount; offset < totalAddresses - 1 && result.Count < count; offset++)
        {
            var ipStr = FromUInt32(baseInt + offset).ToString();
            if (!allocated.Contains(ipStr))
                result.Add(ipStr);
        }

        return result;
    }

    public string? FindNextAvailableIp(string subnetId)
    {
        var (subscriptionIdentifier, resourceGroupIdentifier, vnetName) = ParseSubnetId(subnetId);
        if (vnetName == null) return null;

        var subnetName = GetSubnetName(subnetId);
        var subnet = provider.GetSubresourceAs<SubnetResource>(
            subscriptionIdentifier, resourceGroupIdentifier, subnetName, vnetName, "subnets");
        if (subnet == null) return null;

        var cidr = subnet.Properties.AddressPrefix
            ?? subnet.Properties?.AddressPrefixes?.FirstOrDefault();
        if (cidr == null) return null;

        var available = GetAvailableIps(cidr, subscriptionIdentifier, resourceGroupIdentifier, vnetName, 1);
        return available.Count > 0 ? available[0] : null;
    }

    private static (SubscriptionIdentifier, ResourceGroupIdentifier, string?) ParseSubnetId(string subnetId)
    {
        // Format: /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/virtualNetworks/{vnet}/subnets/{subnet}
        // Indices: [0]="",[1]="subscriptions",[2]={sub},[3]="resourceGroups",[4]={rg},...,[8]={vnet},[9]="subnets",[10]={subnet}
        var segments = subnetId.Split('/');
        if (segments.Length < 11)
            return (SubscriptionIdentifier.From(string.Empty), ResourceGroupIdentifier.From(string.Empty), null);

        return (
            SubscriptionIdentifier.From(segments[2]),
            ResourceGroupIdentifier.From(segments[4]),
            segments[8]);
    }

    private static string GetSubnetName(string subnetId)
    {
        var segments = subnetId.Split('/');
        return segments.Length >= 11 ? segments[10] : string.Empty;
    }

    private static uint ToUInt32(byte[] bytes) =>
        ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];

    private static IPAddress FromUInt32(uint value) =>
        new([(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value]);
}
