using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Topaz.Service.Storage.Models.Responses;

/// <summary>
/// Represents the XML response for the generateUserDelegationKey Blob Storage data-plane operation.
/// The <see cref="Value"/> field holds deterministic key bytes derived from the storage account key
/// and the signed identity fields via HMAC-SHA256. The same derivation is used at SAS validation
/// time so no key persistence is required.
/// </summary>
internal sealed class UserDelegationKeyResponse
{
    public string SignedOid { get; private init; } = string.Empty;
    public string SignedTid { get; private init; } = string.Empty;
    public string SignedStart { get; private init; } = string.Empty;
    public string SignedExpiry { get; private init; } = string.Empty;
    public string SignedService { get; private init; } = "b";
    public string SignedVersion { get; private init; } = string.Empty;
    public string Value { get; private init; } = string.Empty;

    /// <summary>
    /// Derives the user delegation key bytes from the account key and signed fields, then builds
    /// the response. Derivation:
    /// HMAC-SHA256(accountKeyBytes, UTF8(oid + "\n" + tid + "\n" + start + "\n" + expiry + "\n" + service + "\n" + version))
    /// </summary>
    public static UserDelegationKeyResponse FromRequest(
        string start, string expiry, string oid, string tid, string version, string accountKeyBase64)
    {
        var accountKeyBytes = Convert.FromBase64String(accountKeyBase64);
        var derivedKeyBytes = ComputeHmacSha256(accountKeyBytes, string.Join("\n", oid, tid, start, expiry, "b", version));

        return new UserDelegationKeyResponse
        {
            SignedOid = oid,
            SignedTid = tid,
            SignedStart = start,
            SignedExpiry = expiry,
            SignedService = "b",
            SignedVersion = version,
            Value = Convert.ToBase64String(derivedKeyBytes)
        };
    }

    /// <summary>Serializes the response to the Azure Blob Storage REST API XML format.</summary>
    public string ToXml() =>
        new XDocument(
            new XElement("UserDelegationKey",
                new XElement("SignedOid", SignedOid),
                new XElement("SignedTid", SignedTid),
                new XElement("SignedStart", SignedStart),
                new XElement("SignedExpiry", SignedExpiry),
                new XElement("SignedService", SignedService),
                new XElement("SignedVersion", SignedVersion),
                new XElement("Value", Value)
            )
        ).ToString();

    /// <summary>
    /// Computes HMAC-SHA256 of <paramref name="input"/> using <paramref name="keyBytes"/>.
    /// Exposed internally so <see cref="Security.UserDelegationSasValidator"/> can reuse the
    /// same derivation without duplicating the algorithm.
    /// </summary>
    internal static byte[] ComputeHmacSha256(byte[] keyBytes, string input)
    {
        using var hmac = new HMACSHA256(keyBytes);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
    }
}
