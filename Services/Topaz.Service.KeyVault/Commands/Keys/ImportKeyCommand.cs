using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Commands.Keys;

[UsedImplicitly]
[CommandDefinition("keyvault key import", "key-vault", "Imports an externally created key into an Azure Key Vault.")]
[CommandExample("Import an RSA key from a PEM file",
    "topaz keyvault key import --vault-name \"kvlocal\" --name \"my-imported-key\" --pem-file \"/path/to/key.pem\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class ImportKeyCommand(ITopazLogger logger) : Command<ImportKeyCommand.ImportKeyCommandSettings>
{
    public override int Execute(CommandContext context, ImportKeyCommandSettings settings)
    {
        logger.LogInformation($"Executing {nameof(ImportKeyCommand)}.{nameof(Execute)}.");

        var subscriptionIdentifier = SubscriptionIdentifier.From(settings.SubscriptionId!);
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(settings.ResourceGroup!);
        var dataPlane = new KeyVaultKeysDataPlane(logger, new KeyVaultResourceProvider(logger));

        var importRequest = BuildImportRequest(settings.PemFile!);
        var requestJson = JsonSerializer.Serialize(importRequest, GlobalSettings.JsonOptions);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(requestJson));

        var operation = dataPlane.ImportKey(stream, subscriptionIdentifier, resourceGroupIdentifier,
            settings.VaultName!, settings.Name!);

        if (operation.Result == OperationResult.Failed)
        {
            logger.LogError($"({operation.Code}) {operation.Reason}");
            return 1;
        }

        logger.LogInformation(operation.Resource!.ToString());
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ImportKeyCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.VaultName))
            return ValidationResult.Error("Vault name can't be null.");
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Key name can't be null.");
        if (string.IsNullOrEmpty(settings.PemFile))
            return ValidationResult.Error("PEM file path can't be null.");
        if (!File.Exists(settings.PemFile))
            return ValidationResult.Error($"PEM file not found: {settings.PemFile}");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    private static ImportKeyRequest BuildImportRequest(string pemFilePath)
    {
        var pem = File.ReadAllText(pemFilePath);

        // Try RSA first
        using var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(pem);
            var parameters = rsa.ExportParameters(true);
            return new ImportKeyRequest
            {
                Key = new ImportKeyJwk
                {
                    KeyType = "RSA",
                    N = Base64UrlEncode(parameters.Modulus!),
                    E = Base64UrlEncode(parameters.Exponent!),
                    D         = parameters.D        != null ? Base64UrlEncode(parameters.D)        : null,
                    P         = parameters.P        != null ? Base64UrlEncode(parameters.P)        : null,
                    Q         = parameters.Q        != null ? Base64UrlEncode(parameters.Q)        : null,
                    DP        = parameters.DP       != null ? Base64UrlEncode(parameters.DP)       : null,
                    DQ        = parameters.DQ       != null ? Base64UrlEncode(parameters.DQ)       : null,
                    InverseQ  = parameters.InverseQ != null ? Base64UrlEncode(parameters.InverseQ) : null,
                    KeyOperations = ["encrypt", "decrypt", "sign", "verify", "wrapKey", "unwrapKey"]
                }
            };
        }
        catch (CryptographicException) { }

        // Try EC
        using var ec = ECDsa.Create();
        try
        {
            ec.ImportFromPem(pem);
            var parameters = ec.ExportParameters(false);
            var curveName = parameters.Curve.Oid?.FriendlyName switch
            {
                "nistP384" or "ECDSA_P384" => "P-384",
                "nistP521" or "ECDSA_P521" => "P-521",
                _ => "P-256"
            };
            return new ImportKeyRequest
            {
                Key = new ImportKeyJwk
                {
                    KeyType = "EC",
                    Crv = curveName,
                    X = Base64UrlEncode(parameters.Q.X!),
                    Y = Base64UrlEncode(parameters.Q.Y!),
                    KeyOperations = ["sign", "verify"]
                }
            };
        }
        catch (CryptographicException) { }

        throw new InvalidOperationException("Could not parse PEM file as RSA or EC key.");
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [UsedImplicitly]
    public sealed class ImportKeyCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("--vault-name")]
        public string? VaultName { get; set; }

        [CommandOptionDefinition("(Required) Key name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Path to a PEM-encoded RSA or EC key file.", required: true)]
        [CommandOption("--pem-file")]
        public string? PemFile { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
