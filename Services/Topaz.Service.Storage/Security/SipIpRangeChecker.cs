using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace Topaz.Service.Storage.Security;

/// <summary>
/// Checks whether a remote IP address falls within the range specified by an Azure SAS
/// <c>sip=</c> parameter.  The parameter supports either a single IP address or a hyphenated
/// range (<c>start-end</c>, inclusive) for both IPv4 and IPv6.
/// </summary>
internal static class SipIpRangeChecker
{
    /// <summary>
    /// Returns <c>true</c> when the request is allowed by the <c>sip=</c> restriction.
    /// <list type="bullet">
    ///   <item>If <paramref name="sip"/> is null or empty the parameter is absent, so all IPs are allowed.</item>
    ///   <item>If <paramref name="remoteIp"/> is null the IP cannot be determined; the request is denied (fail-closed).</item>
    ///   <item>IPv4-mapped IPv6 addresses (e.g. <c>::ffff:127.0.0.1</c>) are normalised to their IPv4 form before comparison.</item>
    ///   <item>If the address families of the remote IP and the <c>sip=</c> range do not match the request is denied.</item>
    /// </list>
    /// </summary>
    public static bool IsAllowed(string? sip, IPAddress? remoteIp)
    {
        // No restriction in the token.
        if (string.IsNullOrEmpty(sip)) return true;

        // Cannot determine caller IP — fail-closed to match Azure semantics.
        if (remoteIp is null) return false;

        // Normalise IPv4-mapped IPv6 (::ffff:a.b.c.d) to plain IPv4.
        if (remoteIp.IsIPv4MappedToIPv6)
            remoteIp = remoteIp.MapToIPv4();

        // Parse start and end of the sip= range.
        IPAddress start, end;
        var dashIndex = sip.IndexOf('-');
        if (dashIndex >= 0)
        {
            var startStr = sip[..dashIndex];
            var endStr   = sip[(dashIndex + 1)..];
            if (!IPAddress.TryParse(startStr, out start!) || !IPAddress.TryParse(endStr, out end!))
                return false;
        }
        else
        {
            if (!IPAddress.TryParse(sip, out start!))
                return false;
            end = start;
        }

        // Address-family mismatch → deny.
        if (remoteIp.AddressFamily != start.AddressFamily || remoteIp.AddressFamily != end.AddressFamily)
            return false;

        if (remoteIp.AddressFamily == AddressFamily.InterNetwork)
        {
            // IPv4 — convert to uint for range comparison.
            var startVal  = BinaryPrimitives.ReadUInt32BigEndian(start.GetAddressBytes());
            var endVal    = BinaryPrimitives.ReadUInt32BigEndian(end.GetAddressBytes());
            var remoteVal = BinaryPrimitives.ReadUInt32BigEndian(remoteIp.GetAddressBytes());
            return remoteVal >= startVal && remoteVal <= endVal;
        }

        // IPv6 — lexicographic comparison of 16-byte arrays.
        var startBytes  = start.GetAddressBytes();
        var endBytes    = end.GetAddressBytes();
        var remoteBytes = remoteIp.GetAddressBytes();

        return CompareBytes(remoteBytes, startBytes) >= 0
            && CompareBytes(remoteBytes, endBytes)   <= 0;
    }

    private static int CompareBytes(byte[] a, byte[] b)
    {
        for (var i = 0; i < a.Length; i++)
        {
            var diff = a[i] - b[i];
            if (diff != 0) return diff;
        }
        return 0;
    }
}
