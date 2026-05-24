using System.Xml.Linq;
using Topaz.Shared;

namespace Topaz.Service.Storage.Models.Requests;

/// <summary>
/// Represents the XML request body for the generateUserDelegationKey Blob Storage data-plane operation.
/// Azure REST API: POST /?restype=service&amp;comp=userdelegationkey
/// </summary>
internal sealed class GenerateUserDelegationKeyRequest
{
    public string Start { get; private init; } = string.Empty;
    public string Expiry { get; private init; } = string.Empty;

    /// <summary>
    /// Parses a <c>KeyInfo</c> XML request body.
    /// Returns null when the XML is malformed or the required Expiry element is absent.
    /// </summary>
    public static GenerateUserDelegationKeyRequest? FromXml(string xml, ITopazLogger logger)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var root = doc.Root;
            if (root == null) return null;

            var expiry = root.Element("Expiry")?.Value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(expiry)) return null;

            return new GenerateUserDelegationKeyRequest
            {
                Start = root.Element("Start")?.Value ?? string.Empty,
                Expiry = expiry
            };
        }
        catch (Exception ex)
        {
            logger.LogError(nameof(GenerateUserDelegationKeyRequest), nameof(FromXml),
                "Failed to parse KeyInfo XML body: {0}", ex.Message);
            return null;
        }
    }
}
