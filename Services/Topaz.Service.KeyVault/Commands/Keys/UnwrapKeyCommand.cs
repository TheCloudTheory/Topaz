using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.KeyVault.Commands.Keys;

[UsedImplicitly]
[CommandDefinition("keyvault key unwrap", "key-vault", "Unwraps (decrypts) a wrapped key using a Key Vault key (RSA keys only: RSA1_5, RSA-OAEP, RSA-OAEP-256).")]
[CommandExample("Unwrap with RSA-OAEP-256",
    "topaz keyvault key unwrap --vault-name \"kvlocal\" --name \"my-key\" --version \"abc123\" --algorithm \"RSA-OAEP-256\" --value \"<wrapped-base64url>\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class UnwrapKeyCommand(HttpClient httpClient) : TopazHttpCommand<UnwrapKeyCommand.UnwrapKeyCommandSettings>(httpClient)
{

    public override async Task<int> ExecuteAsync(CommandContext context, UnwrapKeyCommandSettings settings)
    {
        var url = $"{KvDataPlaneUrl(settings.VaultName!)}/keys/{settings.Name}/{settings.Version ?? ""}/unwrapkey?api-version=7.4";
        var (success, body) = await PostAsync(url, new { alg = settings.Algorithm, value = settings.Value });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, UnwrapKeyCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.VaultName))
            return ValidationResult.Error("Vault name can't be null.");
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Key name can't be null.");
        if (string.IsNullOrEmpty(settings.Version))
            return ValidationResult.Error("Key version can't be null. Use 'topaz keyvault key get' to retrieve the version.");
        if (string.IsNullOrEmpty(settings.Algorithm))
            return ValidationResult.Error("Algorithm can't be null. Supported: RSA1_5, RSA-OAEP, RSA-OAEP-256.");
        if (string.IsNullOrEmpty(settings.Value))
            return ValidationResult.Error("Wrapped value can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class UnwrapKeyCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("--vault-name")]
        public string? VaultName { get; set; }

        [CommandOptionDefinition("(Required) Key name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Key version.", required: true)]
        [CommandOption("--version")]
        public string? Version { get; set; }

        [CommandOptionDefinition("(Required) Unwrap algorithm. Supported: RSA1_5, RSA-OAEP, RSA-OAEP-256.", required: true)]
        [CommandOption("-a|--algorithm")]
        public string? Algorithm { get; set; }

        [CommandOptionDefinition("(Required) Base64url-encoded wrapped key material to unwrap.", required: true)]
        [CommandOption("--value")]
        public string? Value { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
