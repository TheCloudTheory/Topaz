using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Topaz.Service.KeyVault.Models;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

internal sealed class KeyVaultDataPlane(ITopazLogger logger, KeyVaultResourceProvider provider)
{
    // AES-256-CBC key used to encrypt backup blobs. This is the emulator's vault-specific master key.
    // Structure of an encrypted blob: [9 magic][1 version][16 IV][n ciphertext], then base64url-encoded.
    private static readonly byte[] BackupKey = Encoding.UTF8.GetBytes("Topaz Key Vault Backup Key 2024!");
    private static readonly byte[] BackupMagic = Encoding.UTF8.GetBytes("TOPAZKVBK");
    private const byte BackupVersion = 0x01;

    private static string EncryptBackup(byte[] plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = BackupKey;
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
        aes.Key = BackupKey;
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
                keyBundle = new KeyBundle(keyName, vaultName, keyType, null, null, keyOps, n, e, null, null);
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
                keyBundle = new KeyBundle(keyName, vaultName, keyType, null, jwk.Crv ?? "P-256", keyOps, null, null, x, y);
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
                var parameters = rsa.ExportParameters(false);
                return new KeyBundle(keyName, vaultName, keyType, keySize, null, keyOps,
                    parameters.Modulus, parameters.Exponent, null, null);
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
                var parameters = ec.ExportParameters(false);
                return new KeyBundle(keyName, vaultName, keyType, null, request.Curve ?? "P-256", keyOps,
                    null, null, parameters.Q.X, parameters.Q.Y);
            }
            default:
                // Symmetric / oct keys — no public material exposed.
                return new KeyBundle(keyName, vaultName, keyType, request.KeySize, null, keyOps,
                    null, null, null, null);
        }
    }
}
