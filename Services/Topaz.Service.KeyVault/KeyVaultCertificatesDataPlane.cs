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
    private const string ContactsFileName = "contacts.json";

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
                ? X509CertificateLoader.LoadPkcs12(certBytes, null)
                : X509CertificateLoader.LoadPkcs12(certBytes, request.Password);
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

        var deletedDir = Path.Combine(basePath, DeletedSubDir);
        Directory.CreateDirectory(deletedDir);

        var record = new DeletedCertificateRecord
        {
            CertName = certName,
            Bundles = bundles,
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

    public DataPlaneOperationResult<string> BackupCertificate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string certName)
    {
        PathGuard.ValidateName(certName);
        certName = PathGuard.SanitizeName(certName);

        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(BackupCertificate),
            "Backing up certificate {0} in vault {1}.", certName, vaultName);

        var basePath = GetCertificatesPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, $"{certName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (!File.Exists(entityPath))
            return new DataPlaneOperationResult<string>(OperationResult.NotFound, null,
                $"Certificate {certName} not found.", "CertificateNotFound");

        var bundles = JsonSerializer.Deserialize<CertificateBundle[]>(
            File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!;
        var plaintext = System.Text.Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(bundles, GlobalSettings.JsonOptions));

        var encoded = KeyVaultBackupCipher.EncryptBackup(plaintext);
        return new DataPlaneOperationResult<string>(OperationResult.Success, encoded, null, null);
    }

    public DataPlaneOperationResult<CertificateBundle> RestoreCertificateBackup(
        Stream input,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName)
    {
        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(RestoreCertificateBackup),
            "Restoring certificate backup into vault {0}.", vaultName);

        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();

        if (string.IsNullOrEmpty(rawContent))
            return new DataPlaneOperationResult<CertificateBundle>(OperationResult.Failed, null,
                "Empty request body.", "BadRequest");

        var request = JsonSerializer.Deserialize<RestoreCertificateRequest>(
            rawContent, GlobalSettings.JsonOptions)
                      ?? throw new InvalidOperationException("Invalid request body.");

        if (string.IsNullOrEmpty(request.Value))
            return new DataPlaneOperationResult<CertificateBundle>(OperationResult.Failed, null,
                "Backup value is missing.", "BadRequest");

        var plaintext = KeyVaultBackupCipher.DecryptBackup(request.Value);
        var bundles = JsonSerializer.Deserialize<CertificateBundle[]>(
            System.Text.Encoding.UTF8.GetString(plaintext), GlobalSettings.JsonOptions);

        if (bundles == null || bundles.Length == 0)
            return new DataPlaneOperationResult<CertificateBundle>(OperationResult.Failed, null,
                "Backup contains no certificate versions.", "BadRequest");

        // Name has [JsonIgnore] — extract from the Id: https://.../certificates/{name}/{version}
        var idParts = (bundles[0].Id ?? string.Empty).Split('/');
        var certName = idParts.Length >= 2
            ? PathGuard.SanitizeName(idParts[^2])
            : string.Empty;

        if (string.IsNullOrEmpty(certName))
            return new DataPlaneOperationResult<CertificateBundle>(OperationResult.Failed, null,
                "Could not determine certificate name from backup.", "BadRequest");

        var basePath = GetCertificatesPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, $"{certName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        File.WriteAllText(entityPath, JsonSerializer.Serialize(bundles, GlobalSettings.JsonOptions));

        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(RestoreCertificateBackup),
            "Restored {0} version(s) of certificate {1} into vault {2}.", bundles.Length, certName, vaultName);

        return new DataPlaneOperationResult<CertificateBundle>(OperationResult.Created, bundles.Last(), null, null);
    }

    public DataPlaneOperationResult<DeletedCertificateRecord> GetDeletedCertificate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string certName)
    {
        PathGuard.ValidateName(certName);
        certName = PathGuard.SanitizeName(certName);

        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(GetDeletedCertificate),
            "Getting deleted certificate {0} from vault {1}.", certName, vaultName);

        var basePath = GetCertificatesPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var deletedPath = Path.Combine(basePath, DeletedSubDir, $"{certName}.json");
        PathGuard.EnsureWithinDirectory(deletedPath, basePath);

        if (!File.Exists(deletedPath))
        {
            logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(GetDeletedCertificate),
                "Deleted certificate {0} not found.", certName);
            return new DataPlaneOperationResult<DeletedCertificateRecord>(OperationResult.NotFound, null,
                $"Deleted certificate {certName} not found.", "CertificateNotFound");
        }

        var record = JsonSerializer.Deserialize<DeletedCertificateRecord>(
            File.ReadAllText(deletedPath), GlobalSettings.JsonOptions)!;
        return new DataPlaneOperationResult<DeletedCertificateRecord>(OperationResult.Success, record, null, null);
    }

    public DataPlaneOperationResult<IReadOnlyList<DeletedCertificateRecord>> GetDeletedCertificates(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName)
    {
        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(GetDeletedCertificates),
            "Listing deleted certificates in vault {0}.", vaultName);

        var basePath = GetCertificatesPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var deletedDir = Path.Combine(basePath, DeletedSubDir);

        if (!Directory.Exists(deletedDir))
            return new DataPlaneOperationResult<IReadOnlyList<DeletedCertificateRecord>>(
                OperationResult.Success, [], null, null);

        var records = Directory.GetFiles(deletedDir, "*.json")
            .Select(file => JsonSerializer.Deserialize<DeletedCertificateRecord>(
                File.ReadAllText(file), GlobalSettings.JsonOptions)!)
            .ToList();

        return new DataPlaneOperationResult<IReadOnlyList<DeletedCertificateRecord>>(
            OperationResult.Success, records, null, null);
    }

    public DataPlaneOperationResult<CertificateBundle> RecoverDeletedCertificate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string certName)
    {
        PathGuard.ValidateName(certName);
        certName = PathGuard.SanitizeName(certName);

        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(RecoverDeletedCertificate),
            "Recovering deleted certificate {0} in vault {1}.", certName, vaultName);

        var basePath = GetCertificatesPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var deletedDir = Path.Combine(basePath, DeletedSubDir);
        var deletedPath = Directory.Exists(deletedDir)
            ? Directory.EnumerateFiles(deletedDir, "*.json")
                .FirstOrDefault(f => string.Equals(
                    Path.GetFileNameWithoutExtension(f), certName, StringComparison.Ordinal))
            : null;

        if (deletedPath == null)
        {
            logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(RecoverDeletedCertificate),
                "Deleted certificate {0} not found.", certName);
            return new DataPlaneOperationResult<CertificateBundle>(OperationResult.NotFound, null,
                $"Deleted certificate {certName} not found.", "CertificateNotFound");
        }

        PathGuard.EnsureWithinDirectory(deletedPath, basePath);

        var record = JsonSerializer.Deserialize<DeletedCertificateRecord>(
            File.ReadAllText(deletedPath), GlobalSettings.JsonOptions)!;

        var entityPath = Path.Combine(basePath, $"{certName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        File.WriteAllText(entityPath, JsonSerializer.Serialize(record.Bundles, GlobalSettings.JsonOptions));
        File.Delete(deletedPath);

        var recovered = record.Bundles.Last();
        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(RecoverDeletedCertificate),
            "Recovered certificate {0} in vault {1}.", certName, vaultName);
        return new DataPlaneOperationResult<CertificateBundle>(OperationResult.Success, recovered, null, null);
    }

    public DataPlaneOperationResult PurgeDeletedCertificate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string certName)
    {
        PathGuard.ValidateName(certName);
        certName = PathGuard.SanitizeName(certName);

        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(PurgeDeletedCertificate),
            "Purging deleted certificate {0} from vault {1}.", certName, vaultName);

        var basePath = GetCertificatesPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var deletedDir = Path.Combine(basePath, DeletedSubDir);
        var deletedPath = Directory.Exists(deletedDir)
            ? Directory.EnumerateFiles(deletedDir, "*.json")
                .FirstOrDefault(f => string.Equals(
                    Path.GetFileNameWithoutExtension(f), certName, StringComparison.Ordinal))
            : null;

        if (deletedPath == null)
        {
            logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(PurgeDeletedCertificate),
                "Deleted certificate {0} not found.", certName);
            return new DataPlaneOperationResult(OperationResult.NotFound,
                $"Deleted certificate {certName} not found.", "CertificateNotFound");
        }

        PathGuard.EnsureWithinDirectory(deletedPath, basePath);
        File.Delete(deletedPath);

        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(PurgeDeletedCertificate),
            "Purged certificate {0} from vault {1}.", certName, vaultName);
        return new DataPlaneOperationResult(OperationResult.Deleted, null, null);
    }

    // -------------------------------------------------------------------------
    // Certificate Contacts
    // -------------------------------------------------------------------------

    public DataPlaneOperationResult<CertificateContactsResponse> SetContacts(
        Stream input,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName)
    {
        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(SetContacts),
            "Setting certificate contacts for vault {0}.", vaultName);

        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();
        var request = string.IsNullOrEmpty(rawContent)
            ? new SetCertificateContactsRequest()
            : JsonSerializer.Deserialize<SetCertificateContactsRequest>(rawContent, GlobalSettings.JsonOptions)
              ?? new SetCertificateContactsRequest();

        var contactsPath = GetContactsPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var responseContacts = MapContacts(request.ContactList);
        var response = CertificateContactsResponse.ForVault(vaultName, responseContacts);
        File.WriteAllText(contactsPath, JsonSerializer.Serialize(response, GlobalSettings.JsonOptions));

        return new DataPlaneOperationResult<CertificateContactsResponse>(OperationResult.Success, response, null, null);
    }

    public DataPlaneOperationResult<CertificateContactsResponse> GetContacts(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName)
    {
        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(GetContacts),
            "Getting certificate contacts for vault {0}.", vaultName);

        var contactsPath = GetContactsPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        if (!File.Exists(contactsPath))
            return new DataPlaneOperationResult<CertificateContactsResponse>(
                OperationResult.NotFound, null, "Certificate contacts not found.", "ContactsNotFound");

        var response = JsonSerializer.Deserialize<CertificateContactsResponse>(
            File.ReadAllText(contactsPath), GlobalSettings.JsonOptions)!;
        return new DataPlaneOperationResult<CertificateContactsResponse>(OperationResult.Success, response, null, null);
    }

    public DataPlaneOperationResult<CertificateContactsResponse> DeleteContacts(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName)
    {
        logger.LogDebug(nameof(KeyVaultCertificatesDataPlane), nameof(DeleteContacts),
            "Deleting certificate contacts for vault {0}.", vaultName);

        var contactsPath = GetContactsPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        if (File.Exists(contactsPath))
            File.Delete(contactsPath);

        var response = CertificateContactsResponse.ForVault(vaultName, []);
        return new DataPlaneOperationResult<CertificateContactsResponse>(OperationResult.Success, response, null, null);
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
        var x5T = Base64UrlEncode(hashBytes);

        return new CertificateBundle
        {
            Id = $"https://{kvHost}/certificates/{certName}/{version}",
            Cer = Convert.ToBase64String(cert.RawData),
            X5t = x5T,
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

    private string GetContactsPath(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName)
    {
        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        Directory.CreateDirectory(basePath);
        return Path.Combine(basePath, ContactsFileName);
    }

    private static CertificateContactsResponse.ContactEntry[]? MapContacts(
        SetCertificateContactsRequest.ContactEntry[]? entries)
    {
        return entries?.Select(e => new CertificateContactsResponse.ContactEntry
        {
            EmailAddress = e.EmailAddress,
            Name = e.Name,
            Phone = e.Phone
        }).ToArray();
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
