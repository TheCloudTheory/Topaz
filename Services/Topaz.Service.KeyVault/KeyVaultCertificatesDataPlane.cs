using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Topaz.Service.KeyVault.Models;
using Topaz.Service.KeyVault.Models.Requests.Certificates;
using Topaz.Service.KeyVault.Models.Responses.Certificates;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

internal sealed class KeyVaultCertificatesDataPlane(ITopazLogger logger, KeyVaultResourceProvider provider)
{
    private const string CertificatesSubDir = "certificates";
    private const string DeletedSubDir = "deleted";
    private const string PendingSuffix = ".pending.json";

    public DataPlaneOperationResult<(CertificateBundle Bundle, CertificateOperationResponse Operation)> CreateCertificate(
        Stream input,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string certName)
    {
        PathGuard.ValidateName(certName);
        certName = PathGuard.SanitizeName(certName);

        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(CreateCertificate), "Creating certificate {0} in vault {1}.", certName, vaultName);

        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();
        var request = string.IsNullOrEmpty(rawContent)
            ? new CreateCertificateRequest()
            : JsonSerializer.Deserialize<CreateCertificateRequest>(rawContent, GlobalSettings.JsonOptions)
              ?? new CreateCertificateRequest();

        var policy = request.Policy ?? new CertificatePolicy();
        var keySize = policy.KeyProps?.KeySize ?? 2048;
        var validityMonths = policy.X509Props?.ValidityMonths ?? 12;
        var subject = policy.X509Props?.Subject ?? "CN=DefaultPolicy";
        var issuerName = policy.Issuer?.Name ?? "Self";

        var basePath = GetCertificatesPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, $"{certName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        using var rsa = RSA.Create(keySize);
        var certRequest = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        certRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        certRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(certRequest.PublicKey, false));

        var now = DateTimeOffset.UtcNow;
        var cert = certRequest.CreateSelfSigned(now, now.AddMonths(validityMonths));

        var version = Guid.NewGuid().ToString("N");
        var bundle = BuildBundle(cert, certName, version, vaultName, policy, request.Tags);

        AppendVersion(entityPath, bundle);

        var policyWithId = policy with
        {
            Id = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/certificates/{certName}/policy",
            Attributes = new CertificatePolicy.PolicyAttributes
            {
                Enabled = true,
                Created = now.ToUnixTimeSeconds(),
                Updated = now.ToUnixTimeSeconds()
            }
        };

        var operation = new CertificateOperationResponse
        {
            Id = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/certificates/{certName}/pending",
            Status = "completed",
            StatusDetails = "Certificate has been issued.",
            Target = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/certificates/{certName}",
            Issuer = new CertificateOperationResponse.OperationIssuerParameters { Name = issuerName }
        };

        File.WriteAllText(Path.Combine(basePath, $"{certName}{PendingSuffix}"),
            JsonSerializer.Serialize(operation, GlobalSettings.JsonOptions));

        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(CreateCertificate), "Certificate {0} created in vault {1}.", certName, vaultName);

        // Return bundle with policy populated
        var resultBundle = bundle with { Policy = policyWithId };
        return new DataPlaneOperationResult<(CertificateBundle, CertificateOperationResponse)>(
            OperationResult.Created, (resultBundle, operation), null, null);
    }

    public DataPlaneOperationResult<CertificateBundle> ImportCertificate(
        Stream input,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string certName)
    {
        PathGuard.ValidateName(certName);
        certName = PathGuard.SanitizeName(certName);

        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(ImportCertificate), "Importing certificate {0} in vault {1}.", certName, vaultName);

        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();

        if (string.IsNullOrEmpty(rawContent))
            return new DataPlaneOperationResult<CertificateBundle>(OperationResult.Failed, null, "Empty request body.", "BadRequest");

        var request = JsonSerializer.Deserialize<ImportCertificateRequest>(rawContent, GlobalSettings.JsonOptions)
                      ?? throw new InvalidOperationException("Failed to deserialize ImportCertificateRequest.");

        var certBytes = Convert.FromBase64String(request.Value);
        X509Certificate2 cert;
        try
        {
            cert = string.IsNullOrEmpty(request.Password)
                ? new X509Certificate2(certBytes)
                : new X509Certificate2(certBytes, request.Password);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            return new DataPlaneOperationResult<CertificateBundle>(OperationResult.Failed, null, "Failed to parse certificate.", "BadParameter");
        }

        var policy = request.Policy ?? new CertificatePolicy();
        var basePath = GetCertificatesPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, $"{certName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        var version = Guid.NewGuid().ToString("N");
        var bundle = BuildBundle(cert, certName, version, vaultName, policy, request.Tags);

        AppendVersion(entityPath, bundle);

        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(ImportCertificate), "Certificate {0} imported into vault {1}.", certName, vaultName);
        return new DataPlaneOperationResult<CertificateBundle>(OperationResult.Created, bundle, null, null);
    }

    public DataPlaneOperationResult<CertificateBundle> GetCertificate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string certName, string? version)
    {
        PathGuard.ValidateName(certName);
        certName = PathGuard.SanitizeName(certName);

        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(GetCertificate), "Getting certificate {0} from vault {1}.", certName, vaultName);

        var basePath = GetCertificatesPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, $"{certName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (!File.Exists(entityPath))
            return new DataPlaneOperationResult<CertificateBundle>(OperationResult.NotFound, null, $"Certificate {certName} not found.", "CertificateNotFound");

        var bundles = JsonSerializer.Deserialize<CertificateBundle[]>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!;

        if (string.IsNullOrEmpty(version))
            return new DataPlaneOperationResult<CertificateBundle>(OperationResult.Success, bundles.Last(), null, null);

        var match = bundles.LastOrDefault(b => b.Id != null && b.Id.EndsWith(version, StringComparison.OrdinalIgnoreCase));
        return match == null
            ? new DataPlaneOperationResult<CertificateBundle>(OperationResult.NotFound, null, $"Certificate {certName} version {version} not found.", "CertificateNotFound")
            : new DataPlaneOperationResult<CertificateBundle>(OperationResult.Success, match, null, null);
    }

    public DataPlaneOperationResult<CertificateBundle[]> GetCertificates(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName)
    {
        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(GetCertificates), "Listing certificates in vault {0}.", vaultName);

        var basePath = GetCertificatesPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);

        if (!Directory.Exists(basePath))
            return new DataPlaneOperationResult<CertificateBundle[]>(OperationResult.Success, [], null, null);

        var results = new List<CertificateBundle>();
        foreach (var file in Directory.EnumerateFiles(basePath, "*.json")
                     .Where(f => !f.EndsWith(PendingSuffix, StringComparison.OrdinalIgnoreCase)))
        {
            var bundles = JsonSerializer.Deserialize<CertificateBundle[]>(File.ReadAllText(file), GlobalSettings.JsonOptions);
            if (bundles is { Length: > 0 })
                results.Add(bundles.Last());
        }

        return new DataPlaneOperationResult<CertificateBundle[]>(OperationResult.Success, results.ToArray(), null, null);
    }

    public DataPlaneOperationResult<CertificateBundle[]> GetCertificateVersions(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string certName)
    {
        PathGuard.ValidateName(certName);
        certName = PathGuard.SanitizeName(certName);

        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(GetCertificateVersions), "Getting versions for certificate {0} in vault {1}.", certName, vaultName);

        var basePath = GetCertificatesPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, $"{certName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (!File.Exists(entityPath))
            return new DataPlaneOperationResult<CertificateBundle[]>(OperationResult.NotFound, null, $"Certificate {certName} not found.", "CertificateNotFound");

        var bundles = JsonSerializer.Deserialize<CertificateBundle[]>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!;
        return new DataPlaneOperationResult<CertificateBundle[]>(OperationResult.Success, bundles, null, null);
    }

    public DataPlaneOperationResult<CertificateBundle> UpdateCertificate(
        Stream input,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string certName, string version)
    {
        PathGuard.ValidateName(certName);
        certName = PathGuard.SanitizeName(certName);

        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(UpdateCertificate), "Updating certificate {0} version {1} in vault {2}.", certName, version, vaultName);

        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();
        var request = string.IsNullOrEmpty(rawContent)
            ? null
            : JsonSerializer.Deserialize<UpdateCertificateRequest>(rawContent, GlobalSettings.JsonOptions);

        var basePath = GetCertificatesPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, $"{certName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (!File.Exists(entityPath))
            return new DataPlaneOperationResult<CertificateBundle>(OperationResult.NotFound, null, $"Certificate {certName} not found.", "CertificateNotFound");

        var bundles = JsonSerializer.Deserialize<CertificateBundle[]>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!.ToList();
        var bundle = bundles.LastOrDefault(b => b.Id != null && b.Id.EndsWith(version, StringComparison.OrdinalIgnoreCase));

        if (bundle == null)
            return new DataPlaneOperationResult<CertificateBundle>(OperationResult.NotFound, null, $"Certificate {certName} version {version} not found.", "CertificateNotFound");

        if (request != null)
            bundle.UpdateFromRequest(request);

        File.WriteAllText(entityPath, JsonSerializer.Serialize(bundles.ToArray(), GlobalSettings.JsonOptions));

        return new DataPlaneOperationResult<CertificateBundle>(OperationResult.Updated, bundle, null, null);
    }

    public DataPlaneOperationResult<DeletedCertificateRecord> DeleteCertificate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string certName)
    {
        PathGuard.ValidateName(certName);
        certName = PathGuard.SanitizeName(certName);

        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(DeleteCertificate), "Deleting certificate {0} from vault {1}.", certName, vaultName);

        var basePath = GetCertificatesPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, $"{certName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (!File.Exists(entityPath))
            return new DataPlaneOperationResult<DeletedCertificateRecord>(OperationResult.NotFound, null, $"Certificate {certName} not found.", "CertificateNotFound");

        var bundles = JsonSerializer.Deserialize<CertificateBundle[]>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!;
        var latest = bundles.Last();

        var deletedDir = Path.Combine(basePath, DeletedSubDir);
        Directory.CreateDirectory(deletedDir);

        var record = new DeletedCertificateRecord
        {
            Bundle = latest,
            DeletedDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ScheduledPurgeDate = DateTimeOffset.UtcNow.AddDays(90).ToUnixTimeSeconds()
        };

        File.WriteAllText(Path.Combine(deletedDir, $"{certName}.json"),
            JsonSerializer.Serialize(record, GlobalSettings.JsonOptions));
        File.Delete(entityPath);

        // Clean up pending operation file if present
        var pendingPath = Path.Combine(basePath, $"{certName}{PendingSuffix}");
        if (File.Exists(pendingPath))
            File.Delete(pendingPath);

        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(DeleteCertificate), "Certificate {0} soft-deleted from vault {1}.", certName, vaultName);
        return new DataPlaneOperationResult<DeletedCertificateRecord>(OperationResult.Deleted, record, null, null);
    }

    public DataPlaneOperationResult<CertificateOperationResponse> GetCertificateOperation(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string certName)
    {
        PathGuard.ValidateName(certName);
        certName = PathGuard.SanitizeName(certName);

        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(GetCertificateOperation), "Getting pending operation for certificate {0} in vault {1}.", certName, vaultName);

        var basePath = GetCertificatesPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var pendingPath = Path.Combine(basePath, $"{certName}{PendingSuffix}");
        PathGuard.EnsureWithinDirectory(pendingPath, basePath);

        if (File.Exists(pendingPath))
        {
            var operation = JsonSerializer.Deserialize<CertificateOperationResponse>(
                File.ReadAllText(pendingPath), GlobalSettings.JsonOptions)!;
            return new DataPlaneOperationResult<CertificateOperationResponse>(OperationResult.Success, operation, null, null);
        }

        // Construct a synthetic completed operation from the latest version
        var entityPath = Path.Combine(basePath, $"{certName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (!File.Exists(entityPath))
            return new DataPlaneOperationResult<CertificateOperationResponse>(OperationResult.NotFound, null, $"Certificate {certName} not found.", "CertificateNotFound");

        var syntheticOp = new CertificateOperationResponse
        {
            Id = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/certificates/{certName}/pending",
            Status = "completed",
            Target = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/certificates/{certName}",
            Issuer = new CertificateOperationResponse.OperationIssuerParameters { Name = "Self" }
        };

        return new DataPlaneOperationResult<CertificateOperationResponse>(OperationResult.Success, syntheticOp, null, null);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private string GetCertificatesPath(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName)
    {
        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var certsPath = Path.Combine(basePath, CertificatesSubDir);
        Directory.CreateDirectory(certsPath);
        return certsPath;
    }

    private static void AppendVersion(string entityPath, CertificateBundle bundle)
    {
        if (File.Exists(entityPath))
        {
            var existing = JsonSerializer.Deserialize<CertificateBundle[]>(
                File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!.ToList();
            existing.Add(bundle);
            File.WriteAllText(entityPath, JsonSerializer.Serialize(existing.ToArray(), GlobalSettings.JsonOptions));
        }
        else
        {
            File.WriteAllText(entityPath, JsonSerializer.Serialize(new[] { bundle }, GlobalSettings.JsonOptions));
        }
    }

    private static CertificateBundle BuildBundle(
        X509Certificate2 cert,
        string certName,
        string version,
        string vaultName,
        CertificatePolicy policy,
        Dictionary<string, string>? tags)
    {
        var kvHost = GlobalSettings.GetKeyVaultHost(vaultName);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var thumbprintBytes = cert.GetCertHashString(HashAlgorithmName.SHA1);
        // Convert hex thumbprint to raw bytes then base64url-encode
        var hashBytes = Convert.FromHexString(thumbprintBytes);
        var x5t = Base64UrlEncode(hashBytes);

        return new CertificateBundle
        {
            Id = $"https://{kvHost}/certificates/{certName}/{version}",
            Cer = Convert.ToBase64String(cert.RawData),
            X5t = x5t,
            Kid = $"https://{kvHost}/keys/{certName}/{version}",
            Sid = $"https://{kvHost}/secrets/{certName}/{version}",
            Name = certName,
            Attributes = new CertificateAttributes
            {
                Enabled = true,
                NotBefore = new DateTimeOffset(cert.NotBefore.ToUniversalTime()).ToUnixTimeSeconds(),
                Expires = new DateTimeOffset(cert.NotAfter.ToUniversalTime()).ToUnixTimeSeconds(),
                Created = now,
                Updated = now
            },
            Policy = policy,
            Tags = tags
        };
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
