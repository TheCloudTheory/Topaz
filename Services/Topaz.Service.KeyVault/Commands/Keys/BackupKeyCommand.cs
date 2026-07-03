using System.Text.Json;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Commands.Keys;

[UsedImplicitly]
[CommandDefinition("keyvault key backup", "key-vault", "Backs up a key from an Azure Key Vault.")]
[CommandExample("Backup a key", "topaz keyvault key backup --vault-name \"kvlocal\" --name \"my-key\" --resource-group \"rg-local\" --subscription-id \"36a28ebb-9370-46d8-981c-84efe02048ae\"")]
public class BackupKeyCommand(HttpClient httpClient) : TopazHttpCommand<BackupKeyCommand.BackupKeyCommandSettings>(httpClient)
{

    protected override async Task<int> ExecuteAsync(CommandContext context, BackupKeyCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{KvDataPlaneUrl(settings.VaultName!)}/keys/{settings.Name}/backup?api-version=7.4";
        var (success, body) = await PostAsync(url, new { });
        if (!success) return 1;
        var value = JsonSerializer.Deserialize<JsonElement>(body, GlobalSettings.JsonOptions).GetProperty("value").GetString();
        await Console.Out.WriteLineAsync(value!);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, BackupKeyCommandSettings settings)
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
    public sealed class BackupKeyCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Key Vault name.", required: true)]
        [CommandOption("--vault-name")]
        public string? VaultName { get; set; }

        [CommandOptionDefinition("(Required) Key name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
