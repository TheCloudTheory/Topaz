using System.Net;
using System.Text.Json;
using Topaz.Service.KeyVault.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

internal sealed class KeyVaultDataPlane(ILogger logger, ResourceProvider provider)
{
    private readonly ILogger logger = logger;
    private readonly ResourceProvider provider = provider;

    internal (Secret? data, HttpStatusCode code) SetSecret(Stream input, string vaultName, string secretName)
    {
        this.logger.LogDebug($"Executing {nameof(SetSecret)}: {secretName} {vaultName}");
        
        using var sr = new StreamReader(input);

        var rawContent = sr.ReadToEnd();

        if(string.IsNullOrEmpty(rawContent))
        {
            return (null, HttpStatusCode.Unauthorized);
        }

        var data = JsonSerializer.Deserialize<SetSecretRequest>(rawContent, GlobalSettings.JsonOptions) ?? throw new Exception();

        this.logger.LogDebug($"Executing {nameof(SetSecret)}: Processing {rawContent}.");
        
        var path = this.provider.GetKeyVaultPath(vaultName);
        var fileName = $"{secretName}.json";
        var entityPath = Path.Combine(path, fileName);

        if (File.Exists(entityPath))
        {
            // When SetSecret is called for Key Vault, data plane checks if a secret already exists.
            // If it does, it adds a new version instead of throwing an error or replacing it.
            CreateNewSecretVersion();
            
            return (new Secret(secretName, data.Value), HttpStatusCode.OK);
        }
        
        // Secret does not exist so we simply create it.
        var secret = new Secret(secretName, data.Value);
        File.WriteAllText(entityPath, JsonSerializer.Serialize(secret, GlobalSettings.JsonOptions));

        return (secret, HttpStatusCode.OK);
    }

    private void CreateNewSecretVersion()
    {
        throw new NotImplementedException();
    }

    public (Secret? data, HttpStatusCode code) GetSecret(string vaultName, string secretName)
    {
        this.logger.LogDebug($"Executing {nameof(GetSecret)}: {secretName} {vaultName}");
        
        var path = this.provider.GetKeyVaultPath(vaultName);
        var fileName = $"{secretName}.json";
        var entityPath = Path.Combine(path, fileName);
        
        if (File.Exists(entityPath) == false)
        {
            this.logger.LogDebug($"Executing {nameof(GetSecret)}: Secret {secretName} not found.");
            
            return (null, HttpStatusCode.NotFound);
        }
        
        this.logger.LogDebug($"Executing {nameof(GetSecret)}: Processing {secretName}.");
        
        var data = File.ReadAllText(entityPath);
        var secret = JsonSerializer.Deserialize<Secret>(data, GlobalSettings.JsonOptions);
        
        return (secret, HttpStatusCode.OK);
    }
}
