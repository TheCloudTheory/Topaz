using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.KeyVault.Commands.Keys;

[UsedImplicitly]
[CommandDefinition("keyvault key release", "key-vault", "Releases a key for Secure Key Release (SKR). Any non-empty attestation token is accepted by the emulator.")]
[CommandExample("Release a key",
    "topaz keyvault key release --vault-name \"kvlocal\" --name \"my-key\" --version \"abc123\" --target \"mytoken\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class ReleaseKeyCommand(HttpClient httpClient) : TopazHttpCommand<ReleaseKeyCommand.ReleaseKeyCommandSettings>(httpClient)
{

    protected override async Task<int> ExecuteAsync(CommandContext context, ReleaseKeyCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{KvDataPlaneUrl(settings.VaultName!)}/keys/{settings.Name}/{settings.Version ?? ""}/release?api-version=7.4";
        var (success, body) = await PostAsync(url, new { target = settings.Target, enc = settings.Enc });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, ReleaseKeyCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.VaultName))
            return ValidationResult.Error("Vault name can't be null.");
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Key name can't be null.");
        if (string.IsNullOrEmpty(settings.Version))
            return ValidationResult.Error("Key version can't be null. Use 'topaz keyvault key get' to retrieve the version.");
        if (string.IsNullOrEmpty(settings.Target))
            return ValidationResult.Error("Target attestation token can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ReleaseKeyCommandSettings : CommandSettings
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

        [CommandOptionDefinition("(Required) Attestation assertion (target). Any non-empty value is accepted by the emulator.", required: true)]
        [CommandOption("--target")]
        public string? Target { get; set; }

        [CommandOptionDefinition("(Optional) Encryption algorithm hint. Accepted but not enforced by the emulator.")]
        [CommandOption("--enc")]
        public string? Enc { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
