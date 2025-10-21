using System.Net;
using System.Text.Json;
using Topaz.Service.KeyVault.Models;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.KeyVault.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

internal sealed class KeyVaultDataPlane(ITopazLogger logger, ResourceProvider provider)
{
    internal (Secret? data, HttpStatusCode code) SetSecret(Stream input, string vaultName, string secretName)
    {
        logger.LogDebug($"Executing {nameof(SetSecret)}: {secretName} {vaultName}");
        
        using var sr = new StreamReader(input);

        var rawContent = sr.ReadToEnd();

        if(string.IsNullOrEmpty(rawContent))
        {
            return (null, HttpStatusCode.Unauthorized);
        }

        var data = JsonSerializer.Deserialize<SetSecretRequest>(rawContent, GlobalSettings.JsonOptions) ?? throw new Exception();

        logger.LogDebug($"Executing {nameof(SetSecret)}: Processing {rawContent}.");
        
        var path = provider.GetKeyVaultPath(vaultName);
        var fileName = $"{secretName}.json";
        var entityPath = Path.Combine(path, fileName);

        if (File.Exists(entityPath))
        {
            // When SetSecret is called for Key Vault, data plane checks if a secret already exists.
            // If it does, it adds a new version instead of throwing an error or replacing it.
            var newVersion = CreateNewSecretVersion(secretName, data.Value, entityPath);
            
            return (newVersion, HttpStatusCode.OK);
        }
        
        // Secret does not exist so we simply create it.
        var secret = new Secret(secretName, data.Value, Guid.NewGuid());
        File.WriteAllText(entityPath, JsonSerializer.Serialize(new[] { secret }, GlobalSettings.JsonOptions));

        return (secret, HttpStatusCode.OK);
    }

    private Secret CreateNewSecretVersion(string secretName, string value, string entityPath)
    {
        logger.LogDebug($"Executing {nameof(CreateNewSecretVersion)}: {secretName} {value}");
        
        var secret = new Secret(secretName, value, Guid.NewGuid());
        var data = File.ReadAllText(entityPath);
        var secrets = JsonSerializer.Deserialize<Secret[]>(data, GlobalSettings.JsonOptions)!.ToList();
        
        secrets.Add(secret);
        
        File.WriteAllText(entityPath, JsonSerializer.Serialize(secrets.ToArray(), GlobalSettings.JsonOptions));

        return secret;
    }

    public (Secret? data, HttpStatusCode code) GetSecret(string vaultName, string secretName, string? version)
    {
        logger.LogDebug($"Executing {nameof(GetSecret)}: {secretName} {vaultName}");
        
        var path = provider.GetKeyVaultPath(vaultName);
        var fileName = $"{secretName}.json";
        var entityPath = Path.Combine(path, fileName);
        
        if (!File.Exists(entityPath))
        {
            logger.LogDebug($"Executing {nameof(GetSecret)}: Secret {secretName} not found.");
            
            return (null, HttpStatusCode.NotFound);
        }
        
        logger.LogDebug($"Executing {nameof(GetSecret)}: Processing {secretName}.");
        
        var data = File.ReadAllText(entityPath);
        var secrets = JsonSerializer.Deserialize<Secret[]>(data, GlobalSettings.JsonOptions);

        if (string.IsNullOrEmpty(version))
        {
            return (secrets!.Last(), HttpStatusCode.OK);
        }
        
        var secret = secrets!.LastOrDefault(s => s.Name == secretName && s.Id.EndsWith(version!));
        
        return (secret, secret == null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
    }

    public (Secret[] data, HttpStatusCode code) GetSecrets(string vaultName)
    {
        logger.LogDebug($"Executing {nameof(GetSecrets)}: {vaultName}");
        
        var path = provider.GetKeyVaultPath(vaultName);
        var files = Directory.EnumerateFiles(path, "*.json");
        var secrets = new List<Secret>();

        foreach (var file in files)
        {
            logger.LogDebug($"Executing {nameof(GetSecrets)}: {file}");
            
            var data = File.ReadAllText(file);
            var versions = JsonSerializer.Deserialize<Secret[]>(data, GlobalSettings.JsonOptions);
            var lastVersion = versions!.Last();
            
            secrets.Add(lastVersion);
        }
        
        return (secrets.ToArray(), HttpStatusCode.OK);
    }

    public (Secret? data, HttpStatusCode code) DeleteSecret(string vaultName, string secretName)
    {
        logger.LogDebug($"Executing {nameof(DeleteSecret)}: {secretName} {vaultName}");
        
        var path = provider.GetKeyVaultPath(vaultName);
        var fileName = $"{secretName}.json";
        var entityPath = Path.Combine(path, fileName);
        
        if (File.Exists(entityPath) == false)
        {
            logger.LogDebug($"Executing {nameof(DeleteSecret)}: Secret {secretName} not found.");
            
            return (null, HttpStatusCode.NotFound);
        }
        
        logger.LogDebug($"Executing {nameof(DeleteSecret)}: Processing {secretName}.");
        
        var data = File.ReadAllText(entityPath);
        var secrets = JsonSerializer.Deserialize<Secret[]>(data, GlobalSettings.JsonOptions);
        var secret = secrets!.Last();
        
        logger.LogDebug($"Executing {nameof(DeleteSecret)}: Deleting {secretName}.");
        
        File.Delete(entityPath);
        
        return (secret, HttpStatusCode.OK);
    }
}
