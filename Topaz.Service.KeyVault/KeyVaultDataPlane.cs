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

    internal (Secret? data, HttpStatusCode code) SetSecret(Stream input, string keyVaultName, string secretName)
    {
        this.logger.LogDebug($"Executing {nameof(SetSecret)}: {secretName} {keyVaultName}");

        var path = this.provider.GetKeyVaultPath(keyVaultName);

        using var sr = new StreamReader(input);

        var rawContent = sr.ReadToEnd();
        var metadata = JsonSerializer.Deserialize<SetSecretRequest>(rawContent, GlobalSettings.JsonOptions) ?? throw new Exception();

        this.logger.LogDebug($"Executing {nameof(SetSecret)}: Processing {rawContent}.");

        return (null, HttpStatusCode.OK);
    }
}
