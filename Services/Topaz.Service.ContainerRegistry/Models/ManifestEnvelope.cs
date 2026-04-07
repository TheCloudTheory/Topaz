namespace Topaz.Service.ContainerRegistry.Models;

/// <summary>
/// Wraps a stored OCI/Docker manifest with its content type and pre-computed digest.
/// </summary>
internal sealed class ManifestEnvelope
{
    public string ContentType { get; set; } = "application/vnd.docker.distribution.manifest.v2+json";
    public string Digest { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
}
