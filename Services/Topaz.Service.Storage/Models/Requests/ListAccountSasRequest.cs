using JetBrains.Annotations;

namespace Topaz.Service.Storage.Models.Requests;

[UsedImplicitly]
internal sealed class ListAccountSasRequest
{
    /// <summary>Signed services: b (Blob), q (Queue), t (Table), f (File).</summary>
    public string? SignedServices { get; set; }

    /// <summary>Signed resource types: s (Service), c (Container), o (Object).</summary>
    public string? SignedResourceTypes { get; set; }

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

    /// <summary>Account key name to sign with (key1 or key2).</summary>
    public string? KeyToSign { get; set; }
}
