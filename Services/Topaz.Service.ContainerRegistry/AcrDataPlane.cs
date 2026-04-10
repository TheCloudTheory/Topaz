using System.Security.Cryptography;
using System.Text.Json;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry;

/// <summary>
/// Handles the Docker Registry V2 / OCI Distribution Spec data-plane operations:
/// blob uploads (initiate / patch / complete) and manifest push/pull.
///
/// Storage layout under a registry's <c>data/</c> directory:
/// <code>
///   blobs/sha256/{hex}          — completed blobs, keyed by their digest
///   uploads/{uuid}              — in-progress blob upload chunks (concatenated)
///   manifests/{repository}/{ref} — manifest JSON files (ref = tag or digest hex)
/// </code>
/// </summary>
internal sealed class AcrDataPlane(ContainerRegistryResourceProvider provider, ITopazLogger logger)
{
    // ── Path helpers ──────────────────────────────────────────────────────────

    /// <summary>Returns the root data path for <paramref name="registryName"/> in the given subscription/rg.</summary>
    private string DataPath(SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string registryName)
        => provider.GetServiceInstanceDataPath(sub, rg, registryName);

    private string BlobsPath(SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string registryName)
    {
        var path = Path.Combine(DataPath(sub, rg, registryName), "blobs", "sha256");
        Directory.CreateDirectory(path);
        return path;
    }

    private string UploadsPath(SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string registryName)
    {
        var path = Path.Combine(DataPath(sub, rg, registryName), "uploads");
        Directory.CreateDirectory(path);
        return path;
    }

    private string ManifestsPath(SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string registryName, string repository)
    {
        var path = Path.Combine(DataPath(sub, rg, registryName), "manifests", repository);
        Directory.CreateDirectory(path);
        return path;
    }

    private string ManifestsRootPath(SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string registryName)
    {
        var path = Path.Combine(DataPath(sub, rg, registryName), "manifests");
        Directory.CreateDirectory(path);
        return path;
    }

    // ── Blob upload ───────────────────────────────────────────────────────────

    /// <summary>
    /// Initiates a new blob upload session.
    /// Corresponds to <c>POST /v2/{name}/blobs/uploads/</c>.
    /// Returns the UUID assigned to this upload session.
    /// </summary>
    public string InitiateUpload(SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string registryName)
    {
        logger.LogDebug(nameof(AcrDataPlane), nameof(InitiateUpload),
            "Executing {0}: registry={1}", nameof(InitiateUpload), registryName);

        PathGuard.ValidateName(registryName);

        var uuid = Guid.NewGuid().ToString("D");
        var uploadsDir = UploadsPath(sub, rg, registryName);
        var uploadPath = Path.Combine(uploadsDir, uuid);
        PathGuard.EnsureWithinDirectory(uploadPath, uploadsDir);

        File.WriteAllBytes(uploadPath, []);
        return uuid;
    }

    /// <summary>
    /// Appends a chunk to an in-progress upload session.
    /// Corresponds to <c>PATCH /v2/{name}/blobs/uploads/{uuid}</c>.
    /// Returns the updated byte range (0 to total bytes received so far, inclusive).
    /// </summary>
    public (long start, long end) AppendChunk(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        string registryName, string uuid, Stream chunk)
    {
        logger.LogDebug(nameof(AcrDataPlane), nameof(AppendChunk),
            "Executing {0}: registry={1} uuid={2}", nameof(AppendChunk), registryName, uuid);

        PathGuard.ValidateName(registryName);
        PathGuard.ValidateName(uuid);

        var uploadsDir = UploadsPath(sub, rg, registryName);
        var uploadPath = Path.Combine(uploadsDir, uuid);
        PathGuard.EnsureWithinDirectory(uploadPath, uploadsDir);

        if (!File.Exists(uploadPath))
            throw new FileNotFoundException($"Upload session '{uuid}' not found.");

        using var fs = new FileStream(uploadPath, FileMode.Append, FileAccess.Write, FileShare.None);
        chunk.CopyTo(fs);

        // Use fs.Position (updated as bytes are written) rather than FileInfo.Length
        // (which may not reflect unflushed buffer data). In Append mode the stream
        // starts at EOF, so position after writing == total file length.
        var totalBytes = fs.Position;
        var end = totalBytes == 0 ? 0 : totalBytes - 1;
        return (0, end);
    }

    /// <summary>
    /// Completes a blob upload: optionally appends a final chunk, verifies the digest,
    /// moves the blob into the content-addressable store, and cleans up the session.
    /// Corresponds to <c>PUT /v2/{name}/blobs/uploads/{uuid}?digest=sha256:{hex}</c>.
    /// Returns the final verified digest string (e.g. <c>sha256:abcdef...</c>), or
    /// <c>null</c> when the digest does not match.
    /// </summary>
    public string? CompleteUpload(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        string registryName, string uuid, string digest, Stream? finalChunk)
    {
        logger.LogDebug(nameof(AcrDataPlane), nameof(CompleteUpload),
            "Executing {0}: registry={1} uuid={2} digest={3}", nameof(CompleteUpload), registryName, uuid, digest);

        PathGuard.ValidateName(registryName);
        PathGuard.ValidateName(uuid);

        var uploadsDir = UploadsPath(sub, rg, registryName);
        var uploadPath = Path.Combine(uploadsDir, uuid);
        PathGuard.EnsureWithinDirectory(uploadPath, uploadsDir);

        if (!File.Exists(uploadPath))
            throw new FileNotFoundException($"Upload session '{uuid}' not found.");

        if (finalChunk != null)
        {
            using var fs = new FileStream(uploadPath, FileMode.Append, FileAccess.Write, FileShare.None);
            finalChunk.CopyTo(fs);
        }

        var content = File.ReadAllBytes(uploadPath);
        var actualDigest = ComputeDigest(content);

        if (!string.Equals(actualDigest, digest, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug(nameof(AcrDataPlane), nameof(CompleteUpload),
                "Digest mismatch: expected={0} actual={1}", digest, actualDigest);
            File.Delete(uploadPath);
            return null;
        }

        var blobsDir   = BlobsPath(sub, rg, registryName);
        var digestHex  = actualDigest["sha256:".Length..];
        var blobPath   = Path.Combine(blobsDir, digestHex);
        PathGuard.EnsureWithinDirectory(blobPath, blobsDir);

        File.Move(uploadPath, blobPath, overwrite: true);
        return actualDigest;
    }

    // ── Blob retrieval ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the blob content for the given <paramref name="digest"/>, or <c>null</c> if not found.
    /// Corresponds to <c>GET /v2/{name}/blobs/{digest}</c>.
    /// </summary>
    public byte[]? GetBlob(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        string registryName, string digest)
    {
        logger.LogDebug(nameof(AcrDataPlane), nameof(GetBlob),
            "Executing {0}: registry={1} digest={2}", nameof(GetBlob), registryName, digest);

        PathGuard.ValidateName(registryName);

        var digestHex = SanitizeDigestHex(digest);
        if (digestHex == null) return null;

        var blobsDir = BlobsPath(sub, rg, registryName);
        var blobPath = Path.Combine(blobsDir, digestHex);
        PathGuard.EnsureWithinDirectory(blobPath, blobsDir);

        return File.Exists(blobPath) ? File.ReadAllBytes(blobPath) : null;
    }

    /// <summary>
    /// Returns the byte length of an existing blob, or <c>null</c> when not found.
    /// Corresponds to <c>HEAD /v2/{name}/blobs/{digest}</c>.
    /// </summary>
    public long? GetBlobLength(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        string registryName, string digest)
    {
        logger.LogDebug(nameof(AcrDataPlane), nameof(GetBlobLength),
            "Executing {0}: registry={1} digest={2}", nameof(GetBlobLength), registryName, digest);

        PathGuard.ValidateName(registryName);

        var digestHex = SanitizeDigestHex(digest);
        if (digestHex == null) return null;

        var blobsDir = BlobsPath(sub, rg, registryName);
        var blobPath = Path.Combine(blobsDir, digestHex);
        PathGuard.EnsureWithinDirectory(blobPath, blobsDir);

        if (!File.Exists(blobPath)) return null;
        return new FileInfo(blobPath).Length;
    }

    /// <summary>
    /// Deletes a blob identified by <paramref name="digest"/>.
    /// Corresponds to <c>DELETE /v2/{name}/blobs/{digest}</c>.
    /// Returns <c>true</c> when the blob existed and was deleted; <c>false</c> when not found.
    /// </summary>
    public bool DeleteBlob(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        string registryName, string digest)
    {
        logger.LogDebug(nameof(AcrDataPlane), nameof(DeleteBlob),
            "Executing {0}: registry={1} digest={2}", nameof(DeleteBlob), registryName, digest);

        PathGuard.ValidateName(registryName);

        var digestHex = SanitizeDigestHex(digest);
        if (digestHex == null) return false;

        var blobsDir = BlobsPath(sub, rg, registryName);
        var blobPath = Path.Combine(blobsDir, digestHex);
        PathGuard.EnsureWithinDirectory(blobPath, blobsDir);

        if (!File.Exists(blobPath)) return false;

        File.Delete(blobPath);
        return true;
    }

    /// <summary>
    /// Returns true when a blob with the given <paramref name="digest"/> exists.
    /// Corresponds to <c>HEAD /v2/{name}/blobs/{digest}</c>.
    /// </summary>
    public bool BlobExists(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        string registryName, string digest)
    {
        logger.LogDebug(nameof(AcrDataPlane), nameof(BlobExists),
            "Executing {0}: registry={1} digest={2}", nameof(BlobExists), registryName, digest);

        PathGuard.ValidateName(registryName);

        var digestHex = SanitizeDigestHex(digest);
        if (digestHex == null) return false;

        var blobsDir = BlobsPath(sub, rg, registryName);
        var blobPath = Path.Combine(blobsDir, digestHex);
        PathGuard.EnsureWithinDirectory(blobPath, blobsDir);

        return File.Exists(blobPath);
    }

    // ── Manifests ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Stores a manifest for <paramref name="repository"/> under the given <paramref name="reference"/>
    /// (tag or digest). Also stores a copy keyed by its content digest.
    /// Corresponds to <c>PUT /v2/{name}/manifests/{reference}</c>.
    /// Returns the manifest's content digest.
    /// </summary>
    public string PutManifest(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        string registryName, string repository, string reference,
        byte[] manifestBytes, string contentType)
    {
        logger.LogDebug(nameof(AcrDataPlane), nameof(PutManifest),
            "Executing {0}: registry={1} repository={2} reference={3}", nameof(PutManifest), registryName, repository, reference);

        PathGuard.ValidateName(registryName);
        PathGuard.ValidateName(repository);
        PathGuard.ValidateName(reference);

        var digest = ComputeDigest(manifestBytes);
        var digestHex = digest["sha256:".Length..];

        var manifestsDir = ManifestsPath(sub, rg, registryName, repository);

        // Store by tag or digest reference.
        var refPath = Path.Combine(manifestsDir, reference);
        PathGuard.EnsureWithinDirectory(refPath, manifestsDir);

        var envelope = new ManifestEnvelope { ContentType = contentType, Content = manifestBytes, Digest = digest };
        var json = JsonSerializer.Serialize(envelope, GlobalSettings.JsonOptions);
        File.WriteAllText(refPath + ".json", json);

        // Also index by digest so pulls by digest work.
        if (!reference.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            var digestPath = Path.Combine(manifestsDir, digestHex + ".json");
            PathGuard.EnsureWithinDirectory(digestPath, manifestsDir);
            File.WriteAllText(digestPath, json);
        }

        return digest;
    }

    /// <summary>
    /// Retrieves a manifest envelope for the given <paramref name="reference"/> (tag or digest).
    /// Corresponds to <c>GET /v2/{name}/manifests/{reference}</c>.
    /// Returns <c>null</c> when not found.
    /// </summary>
    public ManifestEnvelope? GetManifest(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        string registryName, string repository, string reference)
    {
        logger.LogDebug(nameof(AcrDataPlane), nameof(GetManifest),
            "Executing {0}: registry={1} repository={2} reference={3}", nameof(GetManifest), registryName, repository, reference);

        PathGuard.ValidateName(registryName);
        PathGuard.ValidateName(repository);
        PathGuard.ValidateName(reference);

        var manifestsDir = ManifestsPath(sub, rg, registryName, repository);

        // Accept "sha256:hex" as a reference.
        var lookupRef = reference.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
            ? reference["sha256:".Length..]
            : reference;

        var refPath = Path.Combine(manifestsDir, lookupRef + ".json");
        PathGuard.EnsureWithinDirectory(refPath, manifestsDir);

        if (!File.Exists(refPath)) return null;

        return JsonSerializer.Deserialize<ManifestEnvelope>(File.ReadAllText(refPath), GlobalSettings.JsonOptions);
    }

    /// <summary>
    /// Deletes the manifest identified by <paramref name="reference"/> (tag or digest) for
    /// <paramref name="repository"/>. When the reference is a tag, the digest-indexed copy is also removed.
    /// Corresponds to <c>DELETE /v2/{name}/manifests/{reference}</c>.
    /// Returns <c>true</c> when the manifest existed and was deleted; <c>false</c> when not found.
    /// </summary>
    public bool DeleteManifest(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        string registryName, string repository, string reference)
    {
        logger.LogDebug(nameof(AcrDataPlane), nameof(DeleteManifest),
            "Executing {0}: registry={1} repository={2} reference={3}", nameof(DeleteManifest), registryName, repository, reference);

        PathGuard.ValidateName(registryName);
        PathGuard.ValidateName(repository);
        PathGuard.ValidateName(reference);

        var manifestsDir = ManifestsPath(sub, rg, registryName, repository);

        // Accept "sha256:hex" as a reference.
        var lookupRef = reference.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
            ? reference["sha256:".Length..]
            : reference;

        var refPath = Path.Combine(manifestsDir, lookupRef + ".json");
        PathGuard.EnsureWithinDirectory(refPath, manifestsDir);

        if (!File.Exists(refPath)) return false;

        // When deleting by tag, also remove the digest-indexed copy.
        if (!reference.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<ManifestEnvelope>(
                    File.ReadAllText(refPath), GlobalSettings.JsonOptions);

                if (envelope?.Digest != null)
                {
                    var digestHex = envelope.Digest["sha256:".Length..];
                    var digestPath = Path.Combine(manifestsDir, digestHex + ".json");
                    PathGuard.EnsureWithinDirectory(digestPath, manifestsDir);
                    if (File.Exists(digestPath)) File.Delete(digestPath);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(nameof(AcrDataPlane), nameof(DeleteManifest),
                    "Could not remove digest-indexed copy: {0}", ex.Message);
            }
        }

        File.Delete(refPath);
        return true;
    }

    /// <summary>
    /// Deletes an entire repository, including all tag and digest indexed manifests.
    /// Corresponds to ACR data-plane <c>DELETE /acr/v1/{name}</c>.
    /// Returns <c>true</c> when the repository existed and was deleted; <c>false</c> when not found.
    /// </summary>
    public bool DeleteRepository(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        string registryName, string repository)
    {
        logger.LogDebug(nameof(AcrDataPlane), nameof(DeleteRepository),
            "Executing {0}: registry={1} repository={2}", nameof(DeleteRepository), registryName, repository);

        PathGuard.ValidateName(registryName);
        PathGuard.ValidateName(repository);

        var manifestsDir = ManifestsPath(sub, rg, registryName, repository);
        if (!Directory.Exists(manifestsDir)) return false;

        Directory.Delete(manifestsDir, recursive: true);
        return true;
    }

    /// <summary>
    /// Returns the sorted list of repository names stored in this registry.
    /// Corresponds to <c>GET /v2/_catalog</c>.
    /// </summary>
    public IReadOnlyList<string> ListRepositories(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string registryName)
    {
        logger.LogDebug(nameof(AcrDataPlane), nameof(ListRepositories),
            "Executing {0}: registry={1}", nameof(ListRepositories), registryName);

        PathGuard.ValidateName(registryName);

        var root = ManifestsRootPath(sub, rg, registryName);

        return Directory.Exists(root)
            ? Directory.GetDirectories(root)
                       .Select(Path.GetFileName)
                       .Where(n => n != null)
                       .Order()
                       .ToList()!
            : [];
    }

    /// <summary>
    /// Returns the sorted list of tag names for <paramref name="repository"/> in this registry.
    /// Digest-only references (64-char lowercase hex filenames) are excluded.
    /// Corresponds to <c>GET /v2/{name}/tags/list</c>.
    /// </summary>
    public IReadOnlyList<string> ListTags(
        SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        string registryName, string repository)
    {
        logger.LogDebug(nameof(AcrDataPlane), nameof(ListTags),
            "Executing {0}: registry={1} repository={2}", nameof(ListTags), registryName, repository);

        PathGuard.ValidateName(registryName);
        PathGuard.ValidateName(repository);

        var dir = ManifestsPath(sub, rg, registryName, repository);

        return Directory.Exists(dir)
            ? Directory.GetFiles(dir, "*.json")
                       .Select(Path.GetFileNameWithoutExtension)
                       .Where(n => n != null && !IsDigestHex(n))
                       .Order()
                       .ToList()!
            : [];
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string ComputeDigest(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>Returns <c>true</c> when <paramref name="name"/> is a bare 64-character lowercase hex digest,
    /// i.e. a digest-indexed manifest file rather than a human-assigned tag.</summary>
    private static bool IsDigestHex(string name)
        => name.Length == 64 && name.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));

    /// <summary>
    /// Extracts the hex portion from a digest string like <c>sha256:abc...</c>.
    /// Returns <c>null</c> if the format is invalid or contains path-traversal characters.
    /// </summary>
    private static string? SanitizeDigestHex(string digest)
    {
        if (!digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            return null;

        var hex = digest["sha256:".Length..];

        // Guard: hex must be purely lowercase hex characters (no path separators).
        if (hex.Length != 64 || !hex.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
            return null;

        return hex;
    }
}
