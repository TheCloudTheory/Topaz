using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.KeyVault.Commands.Keys;

[UsedImplicitly]
[CommandDefinition("keyvault key restore", "key-vault", "Restores a key into an Azure Key Vault from a backup blob.")]
[CommandExample("Restore a key", "topaz keyvault key restore --vault-name \"kvlocal\" --backup-value \"<encoded blob>\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class RestoreKeyCommand(HttpClient httpClient) : TopazHttpCommand<RestoreKeyCommand.RestoreKeyCommandSettings>(httpClient)
{

    protected override async Task<int> ExecuteAsync(CommandContext context, RestoreKeyCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{KvDataPlaneUrl(settings.VaultName!)}/keys/restore?api-version=7.4";
        var (success, body) = await PostAsync(url, new { value = settings.BackupValue });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, RestoreKeyCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.VaultName))
            return ValidationResult.Error("Vault name can't be null.");
        if (string.IsNullOrEmpty(settings.BackupValue))
            return ValidationResult.Error("Backup value can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class RestoreKeyCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("--vault-name")]
        public string? VaultName { get; set; }

        [CommandOptionDefinition("(Required) Base64url-encoded backup blob produced by the backup command.", required: true)]
        [CommandOption("--backup-value")]
        public string? BackupValue { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
