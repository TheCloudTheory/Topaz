using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.KeyVault.Commands.Keys;

[UsedImplicitly]
[CommandDefinition("keyvault key create", "key-vault", "Creates a key in an Azure Key Vault.")]
[CommandExample("Create an RSA key", "topaz keyvault key create --vault-name \"kvlocal\" --name \"my-rsa-key\" --kty RSA --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
[CommandExample("Create an EC key", "topaz keyvault key create --vault-name \"kvlocal\" --name \"my-ec-key\" --kty EC --crv P-256 --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class CreateKeyCommand(HttpClient httpClient) : TopazHttpCommand<CreateKeyCommand.CreateKeyCommandSettings>(httpClient)
{

    public override async Task<int> ExecuteAsync(CommandContext context, CreateKeyCommandSettings settings)
    {
        var url = $"{KvDataPlaneUrl(settings.VaultName!)}/keys/{settings.Name}/create?api-version=7.4";
        var (success, body) = await PostAsync(url, new { kty = settings.KeyType, keySize = settings.KeySize, keyOps = (string[]?)null, crv = settings.Curve });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateKeyCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.VaultName))
            return ValidationResult.Error("Vault name can't be null.");
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Key name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateKeyCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("--vault-name")]
        public string? VaultName { get; set; }

        [CommandOptionDefinition("(Required) Key name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("Key type (RSA, EC). Defaults to RSA.")]
        [CommandOption("--kty")]
        public string? KeyType { get; set; }

        [CommandOptionDefinition("RSA key size in bits (2048, 3072, 4096). Defaults to 2048.")]
        [CommandOption("--key-size")]
        public int? KeySize { get; set; }

        [CommandOptionDefinition("EC curve name (P-256, P-384, P-521). Defaults to P-256 for EC keys.")]
        [CommandOption("--crv")]
        public string? Curve { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
