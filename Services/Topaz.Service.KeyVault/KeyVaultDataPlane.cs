using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Topaz.Service.KeyVault.Models;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.KeyVault.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

internal sealed class KeyVaultDataPlane(ITopazLogger logger, KeyVaultResourceProvider provider)
{
    // AES-256-CBC key used to encrypt backup blobs. This is the emulator's vault-specific master key.
    // Structure of an encrypted blob: [9 magic][1 version][16 IV][n ciphertext], then base64url-encoded.
    private static readonly byte[] BackupEncryptionKey = Convert.FromHexString("546F70617A4B565F42434B5F56312E30546F70617A4B565F42434B5F56312E30");
    private static readonly byte[] BackupMagic = Encoding.UTF8.GetBytes("TOPAZKVBK");
    private const byte BackupVersion = 0x01;

    private static string EncryptBackup(byte[] plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = BackupEncryptionKey;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        var blob = new byte[BackupMagic.Length + 1 + aes.IV.Length + ciphertext.Length];
        BackupMagic.CopyTo(blob, 0);
        blob[BackupMagic.Length] = BackupVersion;
        aes.IV.CopyTo(blob, BackupMagic.Length + 1);
        ciphertext.CopyTo(blob, BackupMagic.Length + 1 + aes.IV.Length);

        return Convert.ToBase64String(blob).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] DecryptBackup(string encoded)
    {
        var padded = encoded.Replace('-', '+').Replace('_', '/');
        var remainder = padded.Length % 4;
        if (remainder == 2) padded += "==";
        else if (remainder == 3) padded += "=";

        var blob = Convert.FromBase64String(padded);
        var headerLength = BackupMagic.Length + 1 + 16;

        if (blob.Length < headerLength)
            throw new InvalidOperationException("Invalid backup blob: too short.");

        for (var i = 0; i < BackupMagic.Length; i++)
            if (blob[i] != BackupMagic[i])
                throw new InvalidOperationException("Invalid backup blob: magic header mismatch.");

        if (blob[BackupMagic.Length] != BackupVersion)
            throw new InvalidOperationException($"Unsupported backup version: {blob[BackupMagic.Length]}.");

        var iv = new byte[16];
        Array.Copy(blob, BackupMagic.Length + 1, iv, 0, 16);
        var ciphertext = new byte[blob.Length - headerLength];
        Array.Copy(blob, headerLength, ciphertext, 0, ciphertext.Length);

        using var aes = Aes.Create();
        aes.Key = BackupEncryptionKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    internal DataPlaneOperationResult<Secret> SetSecret(Stream input, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string vaultName, string secretName)
    {
        PathGuard.ValidateName(secretName);
        secretName = PathGuard.SanitizeName(secretName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(SetSecret), "Executing {0}: {1} {2}", nameof(SetSecret), secretName, vaultName);

        using var sr = new StreamReader(input);

        var rawContent = sr.ReadToEnd();

        if (string.IsNullOrEmpty(rawContent))
        {
            return new DataPlaneOperationResult<Secret>(OperationResult.Failed, null, "Empty request body.", "Unauthorized");
        }

        var data = JsonSerializer.Deserialize<SetSecretRequest>(rawContent, GlobalSettings.JsonOptions) ??
                   throw new Exception();

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(SetSecret), "Executing {0}: Processing {1}.", nameof(SetSecret), rawContent);

        var path = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var fileName = $"{secretName}.json";
        var entityPath = Path.Combine(path, fileName);
        PathGuard.EnsureWithinDirectory(entityPath, path);

        if (File.Exists(entityPath))
        {
            // When SetSecret is called for Key Vault, data plane checks if a secret already exists.
            // If it does, it adds a new version instead of throwing an error or replacing it.
            var newVersion = CreateNewSecretVersion(secretName, data.Value, entityPath, vaultName);

            return new DataPlaneOperationResult<Secret>(OperationResult.Success, newVersion, null, null);
        }

        // Secret does not exists so we simply create it.
        var secret = new Secret(secretName, data.Value, Guid.NewGuid(), vaultName);
        File.WriteAllText(entityPath, JsonSerializer.Serialize(new[] { secret }, GlobalSettings.JsonOptions));

        return new DataPlaneOperationResult<Secret>(OperationResult.Created, secret, null, null);
    }

    private Secret CreateNewSecretVersion(string secretName, string value, string entityPath, string vaultName)
    {
        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(CreateNewSecretVersion), "Executing {0}: {1} {2}", nameof(CreateNewSecretVersion), secretName, value);
        
        var secret = new Secret(secretName, value, Guid.NewGuid(), vaultName);
        var data = File.ReadAllText(entityPath);
        var secrets = JsonSerializer.Deserialize<Secret[]>(data, GlobalSettings.JsonOptions)!.ToList();
        
        secrets.Add(secret);
        
        File.WriteAllText(entityPath, JsonSerializer.Serialize(secrets.ToArray(), GlobalSettings.JsonOptions));

        return secret;
    }

    public DataPlaneOperationResult<Secret> GetSecret(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string vaultName, string secretName, string? version)
    {
        PathGuard.ValidateName(secretName);
        secretName = PathGuard.SanitizeName(secretName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetSecret), "Executing {0}: {1} {2}", nameof(GetSecret), secretName, vaultName);

        var path = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var fileName = $"{secretName}.json";
        var entityPath = Path.Combine(path, fileName);
        PathGuard.EnsureWithinDirectory(entityPath, path);
        
        if (!File.Exists(entityPath))
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetSecret), "Executing {0}: Secret {1} not found.", nameof(GetSecret), secretName);
            
            return new DataPlaneOperationResult<Secret>(OperationResult.NotFound, null, $"Secret {secretName} not found.", "SecretNotFound");
        }
        
        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetSecret), "Executing {0}: Processing {1}.", nameof(GetSecret), secretName);
        
        var data = File.ReadAllText(entityPath);
        var secrets = JsonSerializer.Deserialize<Secret[]>(data, GlobalSettings.JsonOptions);

        if (string.IsNullOrEmpty(version))
        {
            return new DataPlaneOperationResult<Secret>(OperationResult.Success, secrets!.Last(), null, null);
        }
        
        var secret = secrets!.LastOrDefault(s => s.Name == secretName && s.Id.EndsWith(version!));

        return secret == null
            ? new DataPlaneOperationResult<Secret>(OperationResult.NotFound, null, $"Secret {secretName} version {version} not found.", "SecretNotFound")
            : new DataPlaneOperationResult<Secret>(OperationResult.Success, secret, null, null);
    }

    public DataPlaneOperationResult<Secret[]> GetSecrets(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string vaultName)
    {
        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetSecrets), "Executing {0}: {1}", nameof(GetSecrets), vaultName);
        
        var path = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var files = Directory.EnumerateFiles(path, "*.json");
        var secrets = new List<Secret>();

        foreach (var file in files)
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetSecrets), "Executing {0}: {1}", nameof(GetSecrets), file);
            
            var data = File.ReadAllText(file);
            var versions = JsonSerializer.Deserialize<Secret[]>(data, GlobalSettings.JsonOptions);
            var lastVersion = versions!.Last();
            
            secrets.Add(lastVersion);
        }
        
        return new DataPlaneOperationResult<Secret[]>(OperationResult.Success, secrets.ToArray(), null, null);
    }

    public DataPlaneOperationResult<Secret[]> GetSecretVersions(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string vaultName, string secretName)
    {
        PathGuard.ValidateName(secretName);
        secretName = PathGuard.SanitizeName(secretName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetSecretVersions), "Executing {0}: {1} {2}", nameof(GetSecretVersions), secretName, vaultName);

        var path = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var fileName = $"{secretName}.json";
        var entityPath = Path.Combine(path, fileName);
        PathGuard.EnsureWithinDirectory(entityPath, path);

        if (!File.Exists(entityPath))
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetSecretVersions), "Executing {0}: Secret {1} not found.", nameof(GetSecretVersions), secretName);
            return new DataPlaneOperationResult<Secret[]>(OperationResult.NotFound, null, $"Secret {secretName} not found.", "SecretNotFound");
        }

        var data = File.ReadAllText(entityPath);
        var versions = JsonSerializer.Deserialize<Secret[]>(data, GlobalSettings.JsonOptions);

        return new DataPlaneOperationResult<Secret[]>(OperationResult.Success, versions!, null, null);
    }

    public DataPlaneOperationResult<Secret> UpdateSecret(Stream input,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string secretName, string version)
    {
        PathGuard.ValidateName(secretName);
        secretName = PathGuard.SanitizeName(secretName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(UpdateSecret), "Executing {0}: {1} {2}", nameof(UpdateSecret), secretName, vaultName);

        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();

        var request = string.IsNullOrEmpty(rawContent)
            ? null
            : JsonSerializer.Deserialize<UpdateSecretRequest>(rawContent, GlobalSettings.JsonOptions);

        var path = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var fileName = $"{secretName}.json";
        var entityPath = Path.Combine(path, fileName);
        PathGuard.EnsureWithinDirectory(entityPath, path);

        if (!File.Exists(entityPath))
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(UpdateSecret), "Executing {0}: Secret {1} not found.", nameof(UpdateSecret), secretName);
            return new DataPlaneOperationResult<Secret>(OperationResult.NotFound, null, $"Secret {secretName} not found.", "SecretNotFound");
        }

        var data = File.ReadAllText(entityPath);
        var secrets = JsonSerializer.Deserialize<Secret[]>(data, GlobalSettings.JsonOptions)!.ToList();
        var secret = secrets.LastOrDefault(s => s.Id.EndsWith(version));

        if (secret == null)
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(UpdateSecret), "Executing {0}: Secret version {1} not found.", nameof(UpdateSecret), version);
            return new DataPlaneOperationResult<Secret>(OperationResult.NotFound, null, $"Secret {secretName} version {version} not found.", "SecretNotFound");
        }

        secret.UpdateFromRequest(request ?? new UpdateSecretRequest());

        File.WriteAllText(entityPath, JsonSerializer.Serialize(secrets.ToArray(), GlobalSettings.JsonOptions));

        return new DataPlaneOperationResult<Secret>(OperationResult.Updated, secret, null, null);
    }

    public DataPlaneOperationResult<string> BackupSecret(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string vaultName, string secretName)
    {
        PathGuard.ValidateName(secretName);
        secretName = PathGuard.SanitizeName(secretName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(BackupSecret), "Executing {0}: {1} {2}", nameof(BackupSecret), secretName, vaultName);

        var path = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var fileName = $"{secretName}.json";
        var entityPath = Path.Combine(path, fileName);
        PathGuard.EnsureWithinDirectory(entityPath, path);

        if (!File.Exists(entityPath))
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(BackupSecret), "Executing {0}: Secret {1} not found.", nameof(BackupSecret), secretName);
            return new DataPlaneOperationResult<string>(OperationResult.NotFound, null, $"Secret {secretName} not found.", "SecretNotFound");
        }

        // Read all versions and serialize them as the backup payload.
        var data = File.ReadAllText(entityPath);
        var versions = JsonSerializer.Deserialize<Secret[]>(data, GlobalSettings.JsonOptions)!;
        var plaintext = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(versions, GlobalSettings.JsonOptions));

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(BackupSecret), "Executing {0}: Backing up {1} version(s) of secret {2}.", nameof(BackupSecret), versions.Length, secretName);

        var encoded = EncryptBackup(plaintext);
        return new DataPlaneOperationResult<string>(OperationResult.Success, encoded, null, null);
    }

    public DataPlaneOperationResult<Secret> RestoreSecretBackup(Stream input,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string vaultName)
    {
        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(RestoreSecretBackup), "Executing {0}: {1}", nameof(RestoreSecretBackup), vaultName);

        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();

        if (string.IsNullOrEmpty(rawContent))
            return new DataPlaneOperationResult<Secret>(OperationResult.Failed, null, "Empty request body.", "BadRequest");

        var request = JsonSerializer.Deserialize<RestoreSecretRequest>(rawContent, GlobalSettings.JsonOptions)
                      ?? throw new InvalidOperationException("Invalid request body.");

        if (string.IsNullOrEmpty(request.Value))
            return new DataPlaneOperationResult<Secret>(OperationResult.Failed, null, "Backup value is missing.", "BadRequest");

        var plaintext = DecryptBackup(request.Value);
        var versions = JsonSerializer.Deserialize<Secret[]>(Encoding.UTF8.GetString(plaintext), GlobalSettings.JsonOptions);

        if (versions == null || versions.Length == 0)
            return new DataPlaneOperationResult<Secret>(OperationResult.Failed, null, "Backup contains no secret versions.", "BadRequest");

        var secretName = PathGuard.SanitizeName(versions[0].Name);

        var path = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(path, $"{secretName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, path);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(RestoreSecretBackup), "Executing {0}: Restoring {1} version(s) of secret {2}.", nameof(RestoreSecretBackup), versions.Length, secretName);

        File.WriteAllText(entityPath, JsonSerializer.Serialize(versions, GlobalSettings.JsonOptions));

        return new DataPlaneOperationResult<Secret>(OperationResult.Created, versions.Last(), null, null);
    }

    public DataPlaneOperationResult<Secret> DeleteSecret(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string vaultName, string secretName)
    {
        PathGuard.ValidateName(secretName);
        secretName = PathGuard.SanitizeName(secretName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(DeleteSecret), "Executing {0}: {1} {2}", nameof(DeleteSecret), secretName, vaultName);

        var path = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var fileName = $"{secretName}.json";
        var entityPath = Path.Combine(path, fileName);
        PathGuard.EnsureWithinDirectory(entityPath, path);
        
        if (!File.Exists(entityPath))
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(DeleteSecret), "Executing {0}: Secret {1} not found.", nameof(DeleteSecret), secretName);
            
            return new DataPlaneOperationResult<Secret>(OperationResult.NotFound, null, $"Secret {secretName} not found.", "SecretNotFound");
        }
        
        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(DeleteSecret), "Executing {0}: Processing {1}.", nameof(DeleteSecret), secretName);
        
        var data = File.ReadAllText(entityPath);
        var secrets = JsonSerializer.Deserialize<Secret[]>(data, GlobalSettings.JsonOptions);
        var secret = secrets!.Last();
        
        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(DeleteSecret), "Executing {0}: Deleting {1}.", nameof(DeleteSecret), secretName);

        var deletedDir = Path.Combine(path, "deleted");
        Directory.CreateDirectory(deletedDir);

        var record = new DeletedSecretRecord
        {
            Secret = secret,
            DeletedDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
            ScheduledPurgeDate = DateTimeOffset.Now.AddDays(90).ToUnixTimeSeconds()
        };

        File.WriteAllText(Path.Combine(deletedDir, fileName),
            JsonSerializer.Serialize(record, GlobalSettings.JsonOptions));
        File.Delete(entityPath);

        return new DataPlaneOperationResult<Secret>(OperationResult.Deleted, secret, null, null);
    }

    public DataPlaneOperationResult<DeletedSecretRecord> GetDeletedSecret(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string secretName)
    {
        PathGuard.ValidateName(secretName);
        secretName = PathGuard.SanitizeName(secretName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetDeletedSecret), "Executing {0}: {1} {2}", nameof(GetDeletedSecret), secretName, vaultName);

        var path = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var deletedPath = Path.Combine(path, "deleted", $"{secretName}.json");
        PathGuard.EnsureWithinDirectory(deletedPath, path);

        if (!File.Exists(deletedPath))
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetDeletedSecret), "Executing {0}: Deleted secret {1} not found.", nameof(GetDeletedSecret), secretName);
            return new DataPlaneOperationResult<DeletedSecretRecord>(OperationResult.NotFound, null, $"Deleted secret {secretName} not found.", "SecretNotFound");
        }

        var data = File.ReadAllText(deletedPath);
        var record = JsonSerializer.Deserialize<DeletedSecretRecord>(data, GlobalSettings.JsonOptions);

        return new DataPlaneOperationResult<DeletedSecretRecord>(OperationResult.Success, record!, null, null);
    }

    public DataPlaneOperationResult<IReadOnlyList<DeletedSecretRecord>> GetDeletedSecrets(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName)
    {
        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetDeletedSecrets), "Executing {0}: {1}", nameof(GetDeletedSecrets), vaultName);

        var path = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var deletedDir = Path.Combine(path, "deleted");

        if (!Directory.Exists(deletedDir))
            return new DataPlaneOperationResult<IReadOnlyList<DeletedSecretRecord>>(OperationResult.Success, [], null, null);

        var records = Directory.GetFiles(deletedDir, "*.json")
            .Select(file => JsonSerializer.Deserialize<DeletedSecretRecord>(File.ReadAllText(file), GlobalSettings.JsonOptions)!)
            .ToList();

        return new DataPlaneOperationResult<IReadOnlyList<DeletedSecretRecord>>(OperationResult.Success, records, null, null);
    }

    public DataPlaneOperationResult<Secret> RecoverDeletedSecret(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string secretName)
    {
        PathGuard.ValidateName(secretName);
        secretName = PathGuard.SanitizeName(secretName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(RecoverDeletedSecret), "Executing {0}: {1} {2}", nameof(RecoverDeletedSecret), secretName, vaultName);

        var path = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var deletedDir = Path.Combine(path, "deleted");
        var deletedPath = Directory.EnumerateFiles(deletedDir, "*.json")
            .FirstOrDefault(file => string.Equals(
                Path.GetFileNameWithoutExtension(file),
                secretName,
                StringComparison.Ordinal));

        if (deletedPath == null)
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(RecoverDeletedSecret), "Executing {0}: Deleted secret {1} not found.", nameof(RecoverDeletedSecret), secretName);
            return new DataPlaneOperationResult<Secret>(OperationResult.NotFound, null, $"Deleted secret {secretName} not found.", "SecretNotFound");
        }

        PathGuard.EnsureWithinDirectory(deletedPath, path);

        var data = File.ReadAllText(deletedPath);
        var record = JsonSerializer.Deserialize<DeletedSecretRecord>(data, GlobalSettings.JsonOptions)!;
        var secret = record.Secret!;

        var entityPath = Path.Combine(path, $"{secretName}.json");
        File.WriteAllText(entityPath, JsonSerializer.Serialize(new[] { secret }, GlobalSettings.JsonOptions));
        File.Delete(deletedPath);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(RecoverDeletedSecret), "Executing {0}: Recovered secret {1}.", nameof(RecoverDeletedSecret), secretName);
        return new DataPlaneOperationResult<Secret>(OperationResult.Success, secret, null, null);
    }

    /// <summary>Permanently deletes a soft-deleted secret, making it unrecoverable.</summary>
    /// <param name="subscriptionIdentifier">The subscription that owns the vault.</param>
    /// <param name="resourceGroupIdentifier">The resource group that owns the vault.</param>
    /// <param name="vaultName">The name of the vault.</param>
    /// <param name="secretName">The name of the deleted secret to purge.</param>
    /// <returns>A <see cref="DataPlaneOperationResult"/> indicating success or not-found.</returns>
    public DataPlaneOperationResult PurgeDeletedSecret(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string secretName)
    {
        PathGuard.ValidateName(secretName);
        secretName = PathGuard.SanitizeName(secretName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(PurgeDeletedSecret), "Executing {0}: {1} {2}", nameof(PurgeDeletedSecret), secretName, vaultName);

        var path = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var deletedDir = Path.Combine(path, "deleted");
        var deletedPath = Directory.EnumerateFiles(deletedDir, "*.json")
            .FirstOrDefault(file => string.Equals(
                Path.GetFileNameWithoutExtension(file),
                secretName,
                StringComparison.Ordinal));

        if (deletedPath == null)
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(PurgeDeletedSecret), "Executing {0}: Deleted secret {1} not found.", nameof(PurgeDeletedSecret), secretName);
            return new DataPlaneOperationResult(OperationResult.NotFound, $"Deleted secret {secretName} not found.", "SecretNotFound");
        }

        PathGuard.EnsureWithinDirectory(deletedPath, path);

        File.Delete(deletedPath);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(PurgeDeletedSecret), "Executing {0}: Purged secret {1}.", nameof(PurgeDeletedSecret), secretName);
        return new DataPlaneOperationResult(OperationResult.Deleted, null, null);
    }

    public DataPlaneOperationResult<KeyBundle> CreateKey(Stream input,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string keyName)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(CreateKey), "Executing {0}: {1} {2}", nameof(CreateKey), keyName, vaultName);

        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();

        if (string.IsNullOrEmpty(rawContent))
        {
            return new DataPlaneOperationResult<KeyBundle>(OperationResult.Failed, null, "Empty request body.", "BadRequest");
        }

        var request = JsonSerializer.Deserialize<CreateKeyRequest>(rawContent, GlobalSettings.JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize CreateKeyRequest.");

        var keyType = request.KeyType ?? "RSA";
        var keyBundle = GenerateKeyBundle(vaultName, keyName, keyType, request);

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var keysPath = Path.Combine(basePath, "keys");
        Directory.CreateDirectory(keysPath);

        var entityPath = Path.Combine(keysPath, $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (File.Exists(entityPath))
        {
            var existing = JsonSerializer.Deserialize<KeyBundle[]>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!.ToList();
            existing.Add(keyBundle);
            File.WriteAllText(entityPath, JsonSerializer.Serialize(existing.ToArray(), GlobalSettings.JsonOptions));
        }
        else
        {
            File.WriteAllText(entityPath, JsonSerializer.Serialize(new[] { keyBundle }, GlobalSettings.JsonOptions));
        }

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(CreateKey), "Key {0} created in vault {1}.", keyName, vaultName);
        return new DataPlaneOperationResult<KeyBundle>(OperationResult.Created, keyBundle, null, null);
    }

    public DataPlaneOperationResult<KeyBundle> GetKey(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string keyName, string? version)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetKey), "Executing {0}: {1} {2}", nameof(GetKey), keyName, vaultName);

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, "keys", $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (!File.Exists(entityPath))
        {
            return new DataPlaneOperationResult<KeyBundle>(OperationResult.NotFound, null, $"Key {keyName} not found.", "KeyNotFound");
        }

        var bundles = JsonSerializer.Deserialize<KeyBundle[]>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!;

        if (string.IsNullOrEmpty(version))
        {
            return new DataPlaneOperationResult<KeyBundle>(OperationResult.Success, bundles.Last(), null, null);
        }

        var match = bundles.LastOrDefault(b => b.Key.Kid.EndsWith(version, StringComparison.OrdinalIgnoreCase));
        return match == null
            ? new DataPlaneOperationResult<KeyBundle>(OperationResult.NotFound, null, $"Key {keyName} version {version} not found.", "KeyNotFound")
            : new DataPlaneOperationResult<KeyBundle>(OperationResult.Success, match, null, null);
    }

    public DataPlaneOperationResult<KeyBundle[]> GetKeyVersions(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string keyName)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetKeyVersions), "Executing {0}: {1} {2}", nameof(GetKeyVersions), keyName, vaultName);

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, "keys", $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (!File.Exists(entityPath))
        {
            return new DataPlaneOperationResult<KeyBundle[]>(OperationResult.NotFound, null, $"Key {keyName} not found.", "KeyNotFound");
        }

        var bundles = JsonSerializer.Deserialize<KeyBundle[]>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!;
        return new DataPlaneOperationResult<KeyBundle[]>(OperationResult.Success, bundles, null, null);
    }

    public DataPlaneOperationResult<KeyBundle[]> GetKeys(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName)
    {
        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetKeys), "Executing {0}: {1}", nameof(GetKeys), vaultName);

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var keysPath = Path.Combine(basePath, "keys");

        if (!Directory.Exists(keysPath))
            return new DataPlaneOperationResult<KeyBundle[]>(OperationResult.Success, [], null, null);

        var keys = new List<KeyBundle>();
        foreach (var file in Directory.EnumerateFiles(keysPath, "*.json"))
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetKeys), "Reading key file: {0}", file);
            var bundles = JsonSerializer.Deserialize<KeyBundle[]>(File.ReadAllText(file), GlobalSettings.JsonOptions);
            if (bundles is { Length: > 0 })
                keys.Add(bundles.Last());
        }

        return new DataPlaneOperationResult<KeyBundle[]>(OperationResult.Success, keys.ToArray(), null, null);
    }

    public DataPlaneOperationResult<KeyBundle> UpdateKey(Stream input,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string keyName, string version)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(UpdateKey), "Executing {0}: {1} {2}", nameof(UpdateKey), keyName, vaultName);

        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();

        var request = string.IsNullOrEmpty(rawContent)
            ? null
            : JsonSerializer.Deserialize<UpdateKeyRequest>(rawContent, GlobalSettings.JsonOptions);

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, "keys", $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (!File.Exists(entityPath))
            return new DataPlaneOperationResult<KeyBundle>(OperationResult.NotFound, null, $"Key {keyName} not found.", "KeyNotFound");

        var bundles = JsonSerializer.Deserialize<KeyBundle[]>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!.ToList();
        var bundle = bundles.LastOrDefault(b => b.Key.Kid.EndsWith(version, StringComparison.OrdinalIgnoreCase));

        if (bundle == null)
            return new DataPlaneOperationResult<KeyBundle>(OperationResult.NotFound, null, $"Key {keyName} version {version} not found.", "KeyNotFound");

        bundle.UpdateFromRequest(request ?? new UpdateKeyRequest());

        File.WriteAllText(entityPath, JsonSerializer.Serialize(bundles.ToArray(), GlobalSettings.JsonOptions));

        return new DataPlaneOperationResult<KeyBundle>(OperationResult.Updated, bundle, null, null);
    }

    public DataPlaneOperationResult<DeletedKeyRecord> GetDeletedKey(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string keyName)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetDeletedKey), "Executing {0}: {1} {2}", nameof(GetDeletedKey), keyName, vaultName);

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var deletedPath = Path.Combine(basePath, "keys", "deleted", $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(deletedPath, basePath);

        if (!File.Exists(deletedPath))
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetDeletedKey), "Executing {0}: Deleted key {1} not found.", nameof(GetDeletedKey), keyName);
            return new DataPlaneOperationResult<DeletedKeyRecord>(OperationResult.NotFound, null, $"Deleted key {keyName} not found.", "KeyNotFound");
        }

        var data = File.ReadAllText(deletedPath);
        var record = JsonSerializer.Deserialize<DeletedKeyRecord>(data, GlobalSettings.JsonOptions);

        return new DataPlaneOperationResult<DeletedKeyRecord>(OperationResult.Success, record!, null, null);
    }

    public DataPlaneOperationResult<IReadOnlyList<DeletedKeyRecord>> GetDeletedKeys(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName)
    {
        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetDeletedKeys), "Executing {0}: {1}", nameof(GetDeletedKeys), vaultName);

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var deletedDir = Path.Combine(basePath, "keys", "deleted");

        if (!Directory.Exists(deletedDir))
            return new DataPlaneOperationResult<IReadOnlyList<DeletedKeyRecord>>(OperationResult.Success, [], null, null);

        var records = Directory.GetFiles(deletedDir, "*.json")
            .Select(file => JsonSerializer.Deserialize<DeletedKeyRecord>(File.ReadAllText(file), GlobalSettings.JsonOptions)!)
            .ToList();

        return new DataPlaneOperationResult<IReadOnlyList<DeletedKeyRecord>>(OperationResult.Success, records, null, null);
    }

    public DataPlaneOperationResult<KeyBundle> RecoverDeletedKey(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string keyName)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(RecoverDeletedKey), "Executing {0}: {1} {2}", nameof(RecoverDeletedKey), keyName, vaultName);

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var keysPath = Path.Combine(basePath, "keys");
        var deletedDir = Path.Combine(keysPath, "deleted");

        if (!Directory.Exists(deletedDir))
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(RecoverDeletedKey), "Executing {0}: Deleted key {1} not found.", nameof(RecoverDeletedKey), keyName);
            return new DataPlaneOperationResult<KeyBundle>(OperationResult.NotFound, null, $"Deleted key {keyName} not found.", "KeyNotFound");
        }

        var deletedPath = Directory.EnumerateFiles(deletedDir, "*.json")
            .FirstOrDefault(file => string.Equals(
                Path.GetFileNameWithoutExtension(file),
                keyName,
                StringComparison.Ordinal));

        if (deletedPath == null)
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(RecoverDeletedKey), "Executing {0}: Deleted key {1} not found.", nameof(RecoverDeletedKey), keyName);
            return new DataPlaneOperationResult<KeyBundle>(OperationResult.NotFound, null, $"Deleted key {keyName} not found.", "KeyNotFound");
        }

        PathGuard.EnsureWithinDirectory(deletedPath, basePath);

        var record = JsonSerializer.Deserialize<DeletedKeyRecord>(File.ReadAllText(deletedPath), GlobalSettings.JsonOptions)!;
        var bundles = record.Bundles is { Length: > 0 }
            ? record.Bundles
            : record.Bundle is not null
                ? [record.Bundle]
                : throw new InvalidOperationException($"Deleted key record for '{keyName}' did not contain any key versions.");

        var entityPath = Path.Combine(keysPath, $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        File.WriteAllText(entityPath, JsonSerializer.Serialize(bundles, GlobalSettings.JsonOptions));
        File.Delete(deletedPath);

        var recovered = bundles.Last();
        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(RecoverDeletedKey), "Executing {0}: Recovered key {1}.", nameof(RecoverDeletedKey), keyName);
        return new DataPlaneOperationResult<KeyBundle>(OperationResult.Success, recovered, null, null);
    }

    public DataPlaneOperationResult<KeyRotationPolicy> GetKeyRotationPolicy(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string keyName)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetKeyRotationPolicy), "Executing {0}: {1} {2}", nameof(GetKeyRotationPolicy), keyName, vaultName);

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var keyPath = Path.Combine(basePath, "keys", $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(keyPath, basePath);

        if (!File.Exists(keyPath))
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetKeyRotationPolicy), "Key {0} not found.", keyName);
            return new DataPlaneOperationResult<KeyRotationPolicy>(OperationResult.NotFound, null, $"Key {keyName} not found.", "KeyNotFound");
        }

        var policyPath = Path.Combine(basePath, "keys", $"{keyName}.rotationpolicy.json");
        PathGuard.EnsureWithinDirectory(policyPath, basePath);

        if (!File.Exists(policyPath))
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetKeyRotationPolicy), "No rotation policy set for key {0}, returning default.", keyName);
            return new DataPlaneOperationResult<KeyRotationPolicy>(OperationResult.Success, KeyRotationPolicy.Default(vaultName, keyName), null, null);
        }

        var policy = JsonSerializer.Deserialize<KeyRotationPolicy>(File.ReadAllText(policyPath), GlobalSettings.JsonOptions)!;
        return new DataPlaneOperationResult<KeyRotationPolicy>(OperationResult.Success, policy, null, null);
    }

    public DataPlaneOperationResult<KeyRotationPolicy> UpdateKeyRotationPolicy(Stream input,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string keyName)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(UpdateKeyRotationPolicy), "Executing {0}: {1} {2}", nameof(UpdateKeyRotationPolicy), keyName, vaultName);

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var keyPath = Path.Combine(basePath, "keys", $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(keyPath, basePath);

        if (!File.Exists(keyPath))
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(UpdateKeyRotationPolicy), "Key {0} not found.", keyName);
            return new DataPlaneOperationResult<KeyRotationPolicy>(OperationResult.NotFound, null, $"Key {keyName} not found.", "KeyNotFound");
        }

        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();

        if (string.IsNullOrWhiteSpace(rawContent))
            return new DataPlaneOperationResult<KeyRotationPolicy>(OperationResult.BadRequest, null, "Request body cannot be empty.", "BadParameter");

        KeyRotationPolicy? requested;
        try
        {
            requested = JsonSerializer.Deserialize<KeyRotationPolicy>(rawContent, GlobalSettings.JsonOptions);
        }
        catch (JsonException)
        {
            return new DataPlaneOperationResult<KeyRotationPolicy>(OperationResult.BadRequest, null, "Request body is not valid JSON.", "BadParameter");
        }

        if (requested == null)
            return new DataPlaneOperationResult<KeyRotationPolicy>(OperationResult.BadRequest, null, "Request body is not valid JSON.", "BadParameter");

        var policyPath = Path.Combine(basePath, "keys", $"{keyName}.rotationpolicy.json");
        PathGuard.EnsureWithinDirectory(policyPath, basePath);

        // Preserve Created timestamp from the existing policy; ignore client-supplied Id/Created/Updated.
        long created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (File.Exists(policyPath))
        {
            var existing = JsonSerializer.Deserialize<KeyRotationPolicy>(File.ReadAllText(policyPath), GlobalSettings.JsonOptions);
            if (existing?.Attributes != null)
                created = existing.Attributes.Created;
        }

        var policy = new KeyRotationPolicy
        {
            Id = $"https://{GlobalSettings.GetKeyVaultHost(vaultName)}/keys/{keyName}/rotationpolicy",
            Attributes = new KeyRotationPolicyAttributes(
                Created: created,
                Updated: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ExpiryTime: requested.Attributes?.ExpiryTime),
            LifetimeActions = requested.LifetimeActions ?? []
        };

        File.WriteAllText(policyPath, JsonSerializer.Serialize(policy, GlobalSettings.JsonOptions));

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(UpdateKeyRotationPolicy), "Rotation policy updated for key {0}.", keyName);
        return new DataPlaneOperationResult<KeyRotationPolicy>(OperationResult.Updated, policy, null, null);
    }

    public DataPlaneOperationResult<KeyBundle> RotateKey(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string keyName)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(RotateKey), "Executing {0}: {1} {2}", nameof(RotateKey), keyName, vaultName);

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, "keys", $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (!File.Exists(entityPath))
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(RotateKey), "Executing {0}: Key {1} not found.", nameof(RotateKey), keyName);
            return new DataPlaneOperationResult<KeyBundle>(OperationResult.NotFound, null, $"Key {keyName} not found.", "KeyNotFound");
        }

        var bundles = JsonSerializer.Deserialize<KeyBundle[]>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!;
        var latest = bundles.Last();

        // Infer RSA key size from the modulus (N) field length.
        int? keySize = null;
        if (latest.Key.N != null)
        {
            var padded = latest.Key.N.Replace('-', '+').Replace('_', '/');
            padded += new string('=', (4 - padded.Length % 4) % 4);
            keySize = Convert.FromBase64String(padded).Length * 8;
        }

        var request = new CreateKeyRequest
        {
            KeyType = latest.Key.Kty,
            KeySize = keySize,
            Curve = latest.Key.Crv,
            KeyOperations = latest.Key.KeyOps.Length > 0 ? latest.Key.KeyOps : null
        };

        var newBundle = GenerateKeyBundle(vaultName, keyName, latest.Key.Kty, request);

        var updatedBundles = bundles.Append(newBundle).ToArray();
        File.WriteAllText(entityPath, JsonSerializer.Serialize(updatedBundles, GlobalSettings.JsonOptions));

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(RotateKey), "Executing {0}: Rotated key {1} to new version.", nameof(RotateKey), keyName);
        return new DataPlaneOperationResult<KeyBundle>(OperationResult.Success, newBundle, null, null);
    }

    public DataPlaneOperationResult<GetRandomBytesResponse> GetRandomBytes(int count)
    {
        if (count < 1 || count > 128)
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetRandomBytes), "Invalid count {0}: must be between 1 and 128.", count);
            return new DataPlaneOperationResult<GetRandomBytesResponse>(OperationResult.Failed, null, "Count must be between 1 and 128.", "BadParameter");
        }

        var bytes = RandomNumberGenerator.GetBytes(count);
        var base64Url = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetRandomBytes), "Generated {0} random bytes.", count);
        return new DataPlaneOperationResult<GetRandomBytesResponse>(OperationResult.Success, GetRandomBytesResponse.New(base64Url), null, null);
    }

    public DataPlaneOperationResult<KeyOperationResponse> EncryptKey(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string keyName, string keyVersion,
        KeyOperationRequest request)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(EncryptKey), "Executing {0}: {1}/{2} in {3}.", nameof(EncryptKey), keyName, keyVersion, vaultName);

        if (string.IsNullOrEmpty(request.Algorithm))
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null, "Algorithm ('alg') is required.", "BadParameter");

        if (string.IsNullOrEmpty(request.Value))
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null, "Value is required.", "BadParameter");

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, "keys", $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (!File.Exists(entityPath))
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.NotFound, null, $"Key '{keyName}' not found.", "KeyNotFound");

        var bundles = JsonSerializer.Deserialize<KeyBundle[]>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!;
        var bundle = bundles.FirstOrDefault(b => b.Key.Kid.EndsWith($"/{keyVersion}", StringComparison.OrdinalIgnoreCase));
        if (bundle == null)
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.NotFound, null, $"Key version '{keyVersion}' not found.", "KeyNotFound");

        if (!bundle.Attributes.Enabled)
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null, "Key is disabled.", "Forbidden");

        if (bundle.Key.KeyOps.Length > 0 && !bundle.Key.KeyOps.Contains("encrypt", StringComparer.OrdinalIgnoreCase))
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null, "Key does not permit the 'encrypt' operation.", "Forbidden");

        var kty = bundle.Key.Kty?.ToUpperInvariant() ?? string.Empty;
        if (kty is not ("RSA" or "RSA-HSM"))
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null,
                $"Algorithm '{request.Algorithm}' is not supported for key type '{bundle.Key.Kty}'. Only RSA keys support this algorithm.", "BadParameter");

        var padding = request.Algorithm.ToUpperInvariant() switch
        {
            "RSA1_5"       => RSAEncryptionPadding.Pkcs1,
            "RSA-OAEP"     => RSAEncryptionPadding.OaepSHA1,
            "RSA-OAEP-256" => RSAEncryptionPadding.OaepSHA256,
            _ => null
        };

        if (padding == null)
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null,
                $"Algorithm '{request.Algorithm}' is not supported. Supported RSA algorithms: RSA1_5, RSA-OAEP, RSA-OAEP-256.", "BadParameter");

        if (string.IsNullOrEmpty(bundle.Key.N) || string.IsNullOrEmpty(bundle.Key.E))
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null, "RSA public key material (n, e) is missing.", "BadParameter");

        var plaintext = Base64UrlDecode(request.Value);

        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Modulus  = Base64UrlDecode(bundle.Key.N),
            Exponent = Base64UrlDecode(bundle.Key.E)
        });

        var ciphertext = rsa.Encrypt(plaintext, padding);
        var resultBase64Url = Convert.ToBase64String(ciphertext).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(EncryptKey), "Encrypted {0} bytes with {1}.", plaintext.Length, request.Algorithm);
        return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Success,
            KeyOperationResponse.New(bundle.Key.Kid, resultBase64Url), null, null);
    }

    public DataPlaneOperationResult<KeyOperationResponse> DecryptKey(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string keyName, string keyVersion,
        KeyOperationRequest request)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(DecryptKey), "Executing {0}: {1}/{2} in {3}.", nameof(DecryptKey), keyName, keyVersion, vaultName);

        if (string.IsNullOrEmpty(request.Algorithm))
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null, "Algorithm ('alg') is required.", "BadParameter");

        if (string.IsNullOrEmpty(request.Value))
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null, "Value is required.", "BadParameter");

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, "keys", $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (!File.Exists(entityPath))
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.NotFound, null, $"Key '{keyName}' not found.", "KeyNotFound");

        var bundles = JsonSerializer.Deserialize<KeyBundle[]>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!;
        var bundle = bundles.FirstOrDefault(b => b.Key.Kid.EndsWith($"/{keyVersion}", StringComparison.OrdinalIgnoreCase));
        if (bundle == null)
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.NotFound, null, $"Key version '{keyVersion}' not found.", "KeyNotFound");

        if (!bundle.Attributes.Enabled)
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null, "Key is disabled.", "Forbidden");

        if (bundle.Key.KeyOps.Length > 0 && !bundle.Key.KeyOps.Contains("decrypt", StringComparer.OrdinalIgnoreCase))
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null, "Key does not permit the 'decrypt' operation.", "Forbidden");

        var kty = bundle.Key.Kty?.ToUpperInvariant() ?? string.Empty;
        if (kty is not ("RSA" or "RSA-HSM"))
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null,
                $"Algorithm '{request.Algorithm}' is not supported for key type '{bundle.Key.Kty}'. Only RSA keys support this algorithm.", "BadParameter");

        // All six CRT fields are required — macOS (.NET 8) ImportParameters fails without them.
        if (string.IsNullOrEmpty(bundle.Key.D)       || string.IsNullOrEmpty(bundle.Key.P)  ||
            string.IsNullOrEmpty(bundle.Key.Q)       || string.IsNullOrEmpty(bundle.Key.DP) ||
            string.IsNullOrEmpty(bundle.Key.DQ)      || string.IsNullOrEmpty(bundle.Key.InverseQ))
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null,
                "Key version does not contain complete private RSA material. Recreate the key to enable decryption.", "BadParameter");

        var padding = request.Algorithm.ToUpperInvariant() switch
        {
            "RSA1_5"       => RSAEncryptionPadding.Pkcs1,
            "RSA-OAEP"     => RSAEncryptionPadding.OaepSHA1,
            "RSA-OAEP-256" => RSAEncryptionPadding.OaepSHA256,
            _ => null
        };

        if (padding == null)
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null,
                $"Algorithm '{request.Algorithm}' is not supported. Supported RSA algorithms: RSA1_5, RSA-OAEP, RSA-OAEP-256.", "BadParameter");

        try
        {
            var ciphertext = Base64UrlDecode(request.Value);

            using var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus  = Base64UrlDecode(bundle.Key.N!),
                Exponent = Base64UrlDecode(bundle.Key.E!),
                D        = Base64UrlDecode(bundle.Key.D),
                P        = Base64UrlDecode(bundle.Key.P),
                Q        = Base64UrlDecode(bundle.Key.Q),
                DP       = Base64UrlDecode(bundle.Key.DP),
                DQ       = Base64UrlDecode(bundle.Key.DQ),
                InverseQ = Base64UrlDecode(bundle.Key.InverseQ)
            });

            var plaintext = rsa.Decrypt(ciphertext, padding);
            var resultBase64Url = Convert.ToBase64String(plaintext).TrimEnd('=').Replace('+', '-').Replace('/', '_');

            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(DecryptKey), "Decrypted {0} bytes with {1}.", ciphertext.Length, request.Algorithm);
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Success,
                KeyOperationResponse.New(bundle.Key.Kid, resultBase64Url), null, null);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null,
                $"Decryption failed: {ex.Message}", "BadParameter");
        }
    }

    public DataPlaneOperationResult<KeyOperationResponse> SignKey(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string keyName, string keyVersion,
        KeyOperationRequest request)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(SignKey), "Executing {0}: {1}/{2} in {3}.", nameof(SignKey), keyName, keyVersion, vaultName);

        if (string.IsNullOrEmpty(request.Algorithm))
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null, "Algorithm ('alg') is required.", "BadParameter");

        if (string.IsNullOrEmpty(request.Value))
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null, "Value (digest) is required.", "BadParameter");

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, "keys", $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (!File.Exists(entityPath))
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.NotFound, null, $"Key '{keyName}' not found.", "KeyNotFound");

        var bundles = JsonSerializer.Deserialize<KeyBundle[]>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!;
        var bundle = bundles.FirstOrDefault(b => b.Key.Kid.EndsWith($"/{keyVersion}", StringComparison.OrdinalIgnoreCase));
        if (bundle == null)
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.NotFound, null, $"Key version '{keyVersion}' not found.", "KeyNotFound");

        if (!bundle.Attributes.Enabled)
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null, "Key is disabled.", "Forbidden");

        if (bundle.Key.KeyOps.Length > 0 && !bundle.Key.KeyOps.Contains("sign", StringComparer.OrdinalIgnoreCase))
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null, "Key does not permit the 'sign' operation.", "Forbidden");

        var digest = Base64UrlDecode(request.Value);
        var alg = request.Algorithm.ToUpperInvariant();
        var kty = bundle.Key.Kty?.ToUpperInvariant() ?? string.Empty;

        try
        {
            string resultBase64Url;

            if (kty is "RSA" or "RSA-HSM")
            {
                if (string.IsNullOrEmpty(bundle.Key.D))
                    return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null,
                        "Key does not contain private RSA material required for signing.", "BadParameter");

                var (hashAlg, padding) = alg switch
                {
                    "RS256" => (HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                    "RS384" => (HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1),
                    "RS512" => (HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1),
                    "PS256" => (HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
                    "PS384" => (HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
                    "PS512" => (HashAlgorithmName.SHA512, RSASignaturePadding.Pss),
                    _ => ((HashAlgorithmName?)null, (RSASignaturePadding?)null)
                };

                if (padding == null)
                    return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null,
                        $"Algorithm '{request.Algorithm}' is not supported for RSA keys. Supported: RS256, RS384, RS512, PS256, PS384, PS512.", "BadParameter");

                if (string.IsNullOrEmpty(bundle.Key.N) || string.IsNullOrEmpty(bundle.Key.E) ||
                    string.IsNullOrEmpty(bundle.Key.P) || string.IsNullOrEmpty(bundle.Key.Q) ||
                    string.IsNullOrEmpty(bundle.Key.DP) || string.IsNullOrEmpty(bundle.Key.DQ) ||
                    string.IsNullOrEmpty(bundle.Key.InverseQ))
                    return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null,
                        "Key does not contain complete private RSA material. Recreate the key to enable signing.", "BadParameter");

                using var rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters
                {
                    Modulus  = Base64UrlDecode(bundle.Key.N!),
                    Exponent = Base64UrlDecode(bundle.Key.E!),
                    D        = Base64UrlDecode(bundle.Key.D),
                    P        = Base64UrlDecode(bundle.Key.P),
                    Q        = Base64UrlDecode(bundle.Key.Q),
                    DP       = Base64UrlDecode(bundle.Key.DP),
                    DQ       = Base64UrlDecode(bundle.Key.DQ),
                    InverseQ = Base64UrlDecode(bundle.Key.InverseQ)
                });

                var signature = rsa.SignHash(digest, hashAlg!.Value, padding!);
                resultBase64Url = Convert.ToBase64String(signature).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
            else if (kty is "EC" or "EC-HSM")
            {
                if (string.IsNullOrEmpty(bundle.Key.D))
                    return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null,
                        "Key does not contain private EC material required for signing.", "BadParameter");

                if (string.IsNullOrEmpty(bundle.Key.X) || string.IsNullOrEmpty(bundle.Key.Y))
                    return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null,
                        "EC key is missing public coordinates (x, y).", "BadParameter");

                var (hashAlg, curve) = alg switch
                {
                    "ES256" => (HashAlgorithmName.SHA256, ECCurve.NamedCurves.nistP256),
                    "ES384" => (HashAlgorithmName.SHA384, ECCurve.NamedCurves.nistP384),
                    "ES512" => (HashAlgorithmName.SHA512, ECCurve.NamedCurves.nistP521),
                    _ => ((HashAlgorithmName?)null, (ECCurve?)null)
                };

                if (hashAlg == null)
                    return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null,
                        $"Algorithm '{request.Algorithm}' is not supported for EC keys. Supported: ES256, ES384, ES512.", "BadParameter");

                using var ec = ECDsa.Create();
                ec.ImportParameters(new ECParameters
                {
                    Curve = curve!.Value,
                    Q = new ECPoint
                    {
                        X = Base64UrlDecode(bundle.Key.X!),
                        Y = Base64UrlDecode(bundle.Key.Y!)
                    },
                    D = Base64UrlDecode(bundle.Key.D)
                });

                var signature = ec.SignHash(digest, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
                resultBase64Url = Convert.ToBase64String(signature).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
            else
            {
                return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null,
                    $"Sign is not supported for key type '{bundle.Key.Kty}'.", "BadParameter");
            }

            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(SignKey), "Signed digest with {0}.", request.Algorithm);
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Success,
                KeyOperationResponse.New(bundle.Key.Kid, resultBase64Url), null, null);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            return new DataPlaneOperationResult<KeyOperationResponse>(OperationResult.Failed, null,
                $"Sign failed: {ex.Message}", "BadParameter");
        }
    }

    public DataPlaneOperationResult<KeyVerifyResponse> VerifyKey(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string keyName, string keyVersion,
        VerifyKeyRequest request)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(VerifyKey), "Executing {0}: {1}/{2} in {3}.", nameof(VerifyKey), keyName, keyVersion, vaultName);

        if (string.IsNullOrEmpty(request.Alg))
            return new DataPlaneOperationResult<KeyVerifyResponse>(OperationResult.Failed, null, "Algorithm ('alg') is required.", "BadParameter");

        if (string.IsNullOrEmpty(request.Value))
            return new DataPlaneOperationResult<KeyVerifyResponse>(OperationResult.Failed, null, "Value (digest) is required.", "BadParameter");

        if (string.IsNullOrEmpty(request.Signature))
            return new DataPlaneOperationResult<KeyVerifyResponse>(OperationResult.Failed, null, "Signature is required.", "BadParameter");

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, "keys", $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (!File.Exists(entityPath))
            return new DataPlaneOperationResult<KeyVerifyResponse>(OperationResult.NotFound, null, $"Key '{keyName}' not found.", "KeyNotFound");

        var bundles = JsonSerializer.Deserialize<KeyBundle[]>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!;
        var bundle = bundles.FirstOrDefault(b => b.Key.Kid.EndsWith($"/{keyVersion}", StringComparison.OrdinalIgnoreCase));
        if (bundle == null)
            return new DataPlaneOperationResult<KeyVerifyResponse>(OperationResult.NotFound, null, $"Key version '{keyVersion}' not found.", "KeyNotFound");

        if (!bundle.Attributes.Enabled)
            return new DataPlaneOperationResult<KeyVerifyResponse>(OperationResult.Failed, null, "Key is disabled.", "Forbidden");

        if (bundle.Key.KeyOps.Length > 0 && !bundle.Key.KeyOps.Contains("verify", StringComparer.OrdinalIgnoreCase))
            return new DataPlaneOperationResult<KeyVerifyResponse>(OperationResult.Failed, null, "Key does not permit the 'verify' operation.", "Forbidden");

        var digest = Base64UrlDecode(request.Value);
        var signature = Base64UrlDecode(request.Signature);
        var alg = request.Alg.ToUpperInvariant();
        var kty = bundle.Key.Kty?.ToUpperInvariant() ?? string.Empty;

        try
        {
            bool valid;

            if (kty is "RSA" or "RSA-HSM")
            {
                if (string.IsNullOrEmpty(bundle.Key.N) || string.IsNullOrEmpty(bundle.Key.E))
                    return new DataPlaneOperationResult<KeyVerifyResponse>(OperationResult.Failed, null,
                        "RSA public key material (n, e) is missing.", "BadParameter");

                var (hashAlg, padding) = alg switch
                {
                    "RS256" => (HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                    "RS384" => (HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1),
                    "RS512" => (HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1),
                    "PS256" => (HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
                    "PS384" => (HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
                    "PS512" => (HashAlgorithmName.SHA512, RSASignaturePadding.Pss),
                    _ => ((HashAlgorithmName?)null, (RSASignaturePadding?)null)
                };

                if (padding == null)
                    return new DataPlaneOperationResult<KeyVerifyResponse>(OperationResult.Failed, null,
                        $"Algorithm '{request.Alg}' is not supported for RSA keys. Supported: RS256, RS384, RS512, PS256, PS384, PS512.", "BadParameter");

                using var rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters
                {
                    Modulus  = Base64UrlDecode(bundle.Key.N!),
                    Exponent = Base64UrlDecode(bundle.Key.E!)
                });

                valid = rsa.VerifyHash(digest, signature, hashAlg!.Value, padding!);
            }
            else if (kty is "EC" or "EC-HSM")
            {
                if (string.IsNullOrEmpty(bundle.Key.X) || string.IsNullOrEmpty(bundle.Key.Y))
                    return new DataPlaneOperationResult<KeyVerifyResponse>(OperationResult.Failed, null,
                        "EC key is missing public coordinates (x, y).", "BadParameter");

                var (hashAlg, curve) = alg switch
                {
                    "ES256" => (HashAlgorithmName.SHA256, ECCurve.NamedCurves.nistP256),
                    "ES384" => (HashAlgorithmName.SHA384, ECCurve.NamedCurves.nistP384),
                    "ES512" => (HashAlgorithmName.SHA512, ECCurve.NamedCurves.nistP521),
                    _ => ((HashAlgorithmName?)null, (ECCurve?)null)
                };

                if (hashAlg == null)
                    return new DataPlaneOperationResult<KeyVerifyResponse>(OperationResult.Failed, null,
                        $"Algorithm '{request.Alg}' is not supported for EC keys. Supported: ES256, ES384, ES512.", "BadParameter");

                using var ec = ECDsa.Create();
                ec.ImportParameters(new ECParameters
                {
                    Curve = curve!.Value,
                    Q = new ECPoint
                    {
                        X = Base64UrlDecode(bundle.Key.X!),
                        Y = Base64UrlDecode(bundle.Key.Y!)
                    }
                });

                valid = ec.VerifyHash(digest, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            }
            else
            {
                return new DataPlaneOperationResult<KeyVerifyResponse>(OperationResult.Failed, null,
                    $"Verify is not supported for key type '{bundle.Key.Kty}'.", "BadParameter");
            }

            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(VerifyKey), "Verify result for {0}: {1}.", request.Alg, valid);
            return new DataPlaneOperationResult<KeyVerifyResponse>(OperationResult.Success,
                KeyVerifyResponse.New(valid), null, null);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            return new DataPlaneOperationResult<KeyVerifyResponse>(OperationResult.Failed, null,
                $"Verify failed: {ex.Message}", "BadParameter");
        }
    }

    public DataPlaneOperationResult PurgeDeletedKey(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string keyName)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(PurgeDeletedKey), "Executing {0}: {1} {2}", nameof(PurgeDeletedKey), keyName, vaultName);

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var keysPath = Path.Combine(basePath, "keys");
        var deletedDir = Path.Combine(keysPath, "deleted");

        var deletedPath = Directory.Exists(deletedDir)
            ? Directory.EnumerateFiles(deletedDir, "*.json")
                .FirstOrDefault(file => string.Equals(
                    Path.GetFileNameWithoutExtension(file),
                    keyName,
                    StringComparison.Ordinal))
            : null;

        if (deletedPath == null)
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(PurgeDeletedKey), "Executing {0}: Deleted key {1} not found.", nameof(PurgeDeletedKey), keyName);
            return new DataPlaneOperationResult(OperationResult.NotFound, $"Deleted key {keyName} not found.", "KeyNotFound");
        }

        PathGuard.EnsureWithinDirectory(deletedPath, basePath);

        File.Delete(deletedPath);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(PurgeDeletedKey), "Executing {0}: Purged key {1}.", nameof(PurgeDeletedKey), keyName);
        return new DataPlaneOperationResult(OperationResult.Deleted, null, null);
    }

    public DataPlaneOperationResult<DeletedKeyRecord> DeleteKey(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string keyName)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(DeleteKey), "Executing {0}: {1} {2}", nameof(DeleteKey), keyName, vaultName);

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var keysPath = Path.Combine(basePath, "keys");
        var entityPath = Path.Combine(keysPath, $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (!File.Exists(entityPath))
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(DeleteKey), "Executing {0}: Key {1} not found.", nameof(DeleteKey), keyName);
            return new DataPlaneOperationResult<DeletedKeyRecord>(OperationResult.NotFound, null, $"Key {keyName} not found.", "KeyNotFound");
        }

        var bundles = JsonSerializer.Deserialize<KeyBundle[]>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!;
        var latest = bundles.Last();

        var deletedDir = Path.Combine(keysPath, "deleted");
        Directory.CreateDirectory(deletedDir);

        var record = new DeletedKeyRecord
        {
            Bundle = latest,
            Bundles = bundles,
            KeyName = keyName,
            DeletedDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ScheduledPurgeDate = DateTimeOffset.UtcNow.AddDays(90).ToUnixTimeSeconds()
        };

        File.WriteAllText(Path.Combine(deletedDir, $"{keyName}.json"),
            JsonSerializer.Serialize(record, GlobalSettings.JsonOptions));
        File.Delete(entityPath);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(DeleteKey), "Executing {0}: Key {1} soft-deleted.", nameof(DeleteKey), keyName);
        return new DataPlaneOperationResult<DeletedKeyRecord>(OperationResult.Deleted, record, null, null);
    }

    public DataPlaneOperationResult<KeyBundle> ImportKey(Stream input,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string keyName)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(ImportKey), "Executing {0}: {1} {2}", nameof(ImportKey), keyName, vaultName);

        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();

        if (string.IsNullOrEmpty(rawContent))
            return new DataPlaneOperationResult<KeyBundle>(OperationResult.Failed, null, "Empty request body.", "BadRequest");

        var request = JsonSerializer.Deserialize<ImportKeyRequest>(rawContent, GlobalSettings.JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize ImportKeyRequest.");

        if (request.Key == null)
            return new DataPlaneOperationResult<KeyBundle>(OperationResult.Failed, null, "Missing 'key' in request body.", "BadRequest");

        var jwk = request.Key;
        var keyType = jwk.KeyType ?? "RSA";
        var defaultOps = new[] { "encrypt", "decrypt", "sign", "verify", "wrapKey", "unwrapKey" };
        var keyOps = jwk.KeyOperations ?? defaultOps;

        KeyBundle keyBundle;
        switch (keyType.ToUpperInvariant())
        {
            case "RSA":
            case "RSA-HSM":
            {
                if (string.IsNullOrEmpty(jwk.N) || string.IsNullOrEmpty(jwk.E))
                    return new DataPlaneOperationResult<KeyBundle>(OperationResult.Failed, null,
                        "RSA import requires 'n' (modulus) and 'e' (exponent) in the JWK.", "BadRequest");

                var n = Base64UrlDecode(jwk.N);
                var e = Base64UrlDecode(jwk.E);

                // Decode private components when present; strip decrypt/sign/unwrapKey if absent.
                var d         = string.IsNullOrEmpty(jwk.D)        ? null : Base64UrlDecode(jwk.D);
                var p         = string.IsNullOrEmpty(jwk.P)        ? null : Base64UrlDecode(jwk.P);
                var q         = string.IsNullOrEmpty(jwk.Q)        ? null : Base64UrlDecode(jwk.Q);
                var dp        = string.IsNullOrEmpty(jwk.DP)       ? null : Base64UrlDecode(jwk.DP);
                var dq        = string.IsNullOrEmpty(jwk.DQ)       ? null : Base64UrlDecode(jwk.DQ);
                var inverseQ  = string.IsNullOrEmpty(jwk.InverseQ) ? null : Base64UrlDecode(jwk.InverseQ);

                // If no private material, drop operations that require the private key.
                if (d == null)
                    keyOps = keyOps.Except(["decrypt", "sign", "unwrapKey"], StringComparer.OrdinalIgnoreCase).ToArray();

                keyBundle = new KeyBundle(keyName, vaultName, keyType, null, null, keyOps, n, e, null, null,
                    d, p, q, dp, dq, inverseQ);
                break;
            }
            case "EC":
            case "EC-HSM":
            {
                if (string.IsNullOrEmpty(jwk.X) || string.IsNullOrEmpty(jwk.Y))
                    return new DataPlaneOperationResult<KeyBundle>(OperationResult.Failed, null,
                        "EC import requires 'x' and 'y' coordinates in the JWK.", "BadRequest");

                var x = Base64UrlDecode(jwk.X);
                var y = Base64UrlDecode(jwk.Y);
                var ecDBytes = string.IsNullOrEmpty(jwk.D) ? null : Base64UrlDecode(jwk.D);

                // Without the private scalar, signing is not possible.
                if (ecDBytes == null)
                    keyOps = keyOps.Except(["sign"], StringComparer.OrdinalIgnoreCase).ToArray();

                keyBundle = new KeyBundle(keyName, vaultName, keyType, null, jwk.Crv ?? "P-256", keyOps, null, null, x, y, ecD: ecDBytes);
                break;
            }
            default:
                // Symmetric / oct — no public material to validate.
                keyBundle = new KeyBundle(keyName, vaultName, keyType, null, null, keyOps, null, null, null, null);
                break;
        }

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var keysPath = Path.Combine(basePath, "keys");
        Directory.CreateDirectory(keysPath);

        var entityPath = Path.Combine(keysPath, $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (File.Exists(entityPath))
        {
            var existing = JsonSerializer.Deserialize<KeyBundle[]>(File.ReadAllText(entityPath), GlobalSettings.JsonOptions)!.ToList();
            existing.Add(keyBundle);
            File.WriteAllText(entityPath, JsonSerializer.Serialize(existing.ToArray(), GlobalSettings.JsonOptions));
        }
        else
        {
            File.WriteAllText(entityPath, JsonSerializer.Serialize(new[] { keyBundle }, GlobalSettings.JsonOptions));
        }

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(ImportKey), "Key {0} imported into vault {1}.", keyName, vaultName);
        return new DataPlaneOperationResult<KeyBundle>(OperationResult.Created, keyBundle, null, null);
    }

    public DataPlaneOperationResult<string> BackupKey(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string vaultName, string keyName)
    {
        PathGuard.ValidateName(keyName);
        keyName = PathGuard.SanitizeName(keyName);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(BackupKey), "Executing {0}: {1} {2}", nameof(BackupKey), keyName, vaultName);

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var entityPath = Path.Combine(basePath, "keys", $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        if (!File.Exists(entityPath))
        {
            logger.LogDebug(nameof(KeyVaultDataPlane), nameof(BackupKey), "Executing {0}: Key {1} not found.", nameof(BackupKey), keyName);
            return new DataPlaneOperationResult<string>(OperationResult.NotFound, null, $"Key {keyName} not found.", "KeyNotFound");
        }

        var data = File.ReadAllText(entityPath);
        var versions = JsonSerializer.Deserialize<KeyBundle[]>(data, GlobalSettings.JsonOptions)!;
        var plaintext = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(versions, GlobalSettings.JsonOptions));

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(BackupKey), "Executing {0}: Backing up {1} version(s) of key {2}.", nameof(BackupKey), versions.Length, keyName);

        var encoded = EncryptBackup(plaintext);
        return new DataPlaneOperationResult<string>(OperationResult.Success, encoded, null, null);
    }

    public DataPlaneOperationResult<KeyBundle> RestoreKeyBackup(Stream input,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string vaultName)
    {
        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(RestoreKeyBackup), "Executing {0}: {1}", nameof(RestoreKeyBackup), vaultName);

        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();

        if (string.IsNullOrEmpty(rawContent))
            return new DataPlaneOperationResult<KeyBundle>(OperationResult.Failed, null, "Empty request body.", "BadRequest");

        var request = JsonSerializer.Deserialize<RestoreKeyRequest>(rawContent, GlobalSettings.JsonOptions)
                      ?? throw new InvalidOperationException("Invalid request body.");

        if (string.IsNullOrEmpty(request.Value))
            return new DataPlaneOperationResult<KeyBundle>(OperationResult.Failed, null, "Backup value is missing.", "BadRequest");

        var plaintext = DecryptBackup(request.Value);
        var versions = JsonSerializer.Deserialize<KeyBundle[]>(Encoding.UTF8.GetString(plaintext), GlobalSettings.JsonOptions);

        if (versions == null || versions.Length == 0)
            return new DataPlaneOperationResult<KeyBundle>(OperationResult.Failed, null, "Backup contains no key versions.", "BadRequest");

        // Name is [JsonIgnore] on KeyBundle, so extract it from the Kid URL:
        // "https://{host}/keys/{keyName}/{version}" → second-to-last path segment.
        var kid = versions[0].Key?.Kid ?? string.Empty;
        var kidParts = kid.Split('/');
        var rawKeyName = kidParts.Length >= 2 ? kidParts[^2] : string.Empty;
        var keyName = PathGuard.SanitizeName(rawKeyName);

        var basePath = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var keysPath = Path.Combine(basePath, "keys");
        Directory.CreateDirectory(keysPath);
        var entityPath = Path.Combine(keysPath, $"{keyName}.json");
        PathGuard.EnsureWithinDirectory(entityPath, basePath);

        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(RestoreKeyBackup), "Executing {0}: Restoring {1} version(s) of key {2}.", nameof(RestoreKeyBackup), versions.Length, keyName);

        File.WriteAllText(entityPath, JsonSerializer.Serialize(versions, GlobalSettings.JsonOptions));

        return new DataPlaneOperationResult<KeyBundle>(OperationResult.Created, versions.Last(), null, null);
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        var padding = s.Length % 4;
        if (padding == 2) s += "==";
        else if (padding == 3) s += "=";
        return Convert.FromBase64String(s);
    }

    private static KeyBundle GenerateKeyBundle(string vaultName, string keyName, string keyType,
        CreateKeyRequest request)
    {
        var defaultOps = new[] { "encrypt", "decrypt", "sign", "verify", "wrapKey", "unwrapKey" };
        var keyOps = request.KeyOperations ?? defaultOps;

        switch (keyType.ToUpperInvariant())
        {
            case "RSA":
            case "RSA-HSM":
            {
                var keySize = request.KeySize ?? 2048;
                using var rsa = RSA.Create(keySize);
                var parameters = rsa.ExportParameters(true);
                return new KeyBundle(keyName, vaultName, keyType, keySize, null, keyOps,
                    parameters.Modulus, parameters.Exponent,
                    null, null,
                    parameters.D, parameters.P, parameters.Q,
                    parameters.DP, parameters.DQ, parameters.InverseQ);
            }
            case "EC":
            case "EC-HSM":
            {
                var curve = (request.Curve ?? "P-256") switch
                {
                    "P-384" => ECCurve.NamedCurves.nistP384,
                    "P-521" => ECCurve.NamedCurves.nistP521,
                    _       => ECCurve.NamedCurves.nistP256,
                };
                using var ec = ECDsa.Create(curve);
                var parameters = ec.ExportParameters(true);
                return new KeyBundle(keyName, vaultName, keyType, null, request.Curve ?? "P-256", keyOps,
                    null, null, parameters.Q.X, parameters.Q.Y, ecD: parameters.D);
            }
            default:
                // Symmetric / oct keys — no public material exposed.
                return new KeyBundle(keyName, vaultName, keyType, request.KeySize, null, keyOps,
                    null, null, null, null);
        }
    }
}
