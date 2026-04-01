using System.Net;
using System.Text.Json;
using Topaz.Service.KeyVault.Models;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

internal sealed class KeyVaultDataPlane(ITopazLogger logger, KeyVaultResourceProvider provider)
{
    internal DataPlaneOperationResult<Secret> SetSecret(Stream input, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string vaultName, string secretName)
    {
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

        if (File.Exists(entityPath))
        {
            // When SetSecret is called for Key Vault, data plane checks if a secret already exists.
            // If it does, it adds a new version instead of throwing an error or replacing it.
            var newVersion = CreateNewSecretVersion(secretName, data.Value, entityPath);

            return new DataPlaneOperationResult<Secret>(OperationResult.Success, newVersion, null, null);
        }

        // Secret does not exist so we simply create it.
        var secret = new Secret(secretName, data.Value, Guid.NewGuid());
        File.WriteAllText(entityPath, JsonSerializer.Serialize(new[] { secret }, GlobalSettings.JsonOptions));

        return new DataPlaneOperationResult<Secret>(OperationResult.Created, secret, null, null);
    }

    private Secret CreateNewSecretVersion(string secretName, string value, string entityPath)
    {
        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(CreateNewSecretVersion), "Executing {0}: {1} {2}", nameof(CreateNewSecretVersion), secretName, value);
        
        var secret = new Secret(secretName, value, Guid.NewGuid());
        var data = File.ReadAllText(entityPath);
        var secrets = JsonSerializer.Deserialize<Secret[]>(data, GlobalSettings.JsonOptions)!.ToList();
        
        secrets.Add(secret);
        
        File.WriteAllText(entityPath, JsonSerializer.Serialize(secrets.ToArray(), GlobalSettings.JsonOptions));

        return secret;
    }

    public DataPlaneOperationResult<Secret> GetSecret(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string vaultName, string secretName, string? version)
    {
        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(GetSecret), "Executing {0}: {1} {2}", nameof(GetSecret), secretName, vaultName);
        
        var path = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var fileName = $"{secretName}.json";
        var entityPath = Path.Combine(path, fileName);
        
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

    public DataPlaneOperationResult<Secret> UpdateSecret(Stream input,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string vaultName, string secretName, string version)
    {
        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(UpdateSecret), "Executing {0}: {1} {2}", nameof(UpdateSecret), secretName, vaultName);

        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();

        var request = string.IsNullOrEmpty(rawContent)
            ? null
            : JsonSerializer.Deserialize<UpdateSecretRequest>(rawContent, GlobalSettings.JsonOptions);

        var path = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var fileName = $"{secretName}.json";
        var entityPath = Path.Combine(path, fileName);

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

    public DataPlaneOperationResult<Secret> DeleteSecret(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string vaultName, string secretName)
    {
        logger.LogDebug(nameof(KeyVaultDataPlane), nameof(DeleteSecret), "Executing {0}: {1} {2}", nameof(DeleteSecret), secretName, vaultName);
        
        var path = provider.GetServiceInstanceDataPath(subscriptionIdentifier, resourceGroupIdentifier, vaultName);
        var fileName = $"{secretName}.json";
        var entityPath = Path.Combine(path, fileName);
        
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
        
        File.Delete(entityPath);
        
        return new DataPlaneOperationResult<Secret>(OperationResult.Deleted, secret, null, null);
    }
}
