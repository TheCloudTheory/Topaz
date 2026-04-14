using JetBrains.Annotations;

namespace Topaz.Service.Storage.Models.Requests;

[UsedImplicitly]
internal sealed class ListServiceSasRequest
{
    /// <summary>The canonical resource path, e.g. /blob/{accountName}/{containerName}.</summary>
    public string? CanonicalizedResource { get; set; }

    /// <summary>Signed resource type: b (blob), c (container), f (file), s (share).</summary>
    public string? SignedResource { get; set; }

    /// <summary>Signed permissions: r, w, d, l, a, c, u, p.</summary>
    public string? SignedPermission { get; set; }

    /// <summary>IP address or range.</summary>
    public string? SignedIp { get; set; }

    /// <summary>Allowed protocol: "https" or "https,http".</summary>
    public string? SignedProtocol { get; set; }

    /// <summary>ISO 8601 UTC start time (optional).</summary>
    public string? SignedStart { get; set; }

    /// <summary>ISO 8601 UTC expiry time (required).</summary>
    public string? SignedExpiry { get; set; }

    /// <summary>Stored access policy identifier (optional).</summary>
    public string? SignedIdentifier { get; set; }

    /// <summary>Account key name to sign with (key1 or key2).</summary>
    public string? KeyToSign { get; set; }
}
