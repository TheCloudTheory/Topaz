using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage account keys renew", "azure-storage/account", "Regenerates an access key for a storage account.")]
[CommandExample("Regenerate key1", "topaz storage account keys renew \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"salocal\" \\\n    --key-name \"key1\"")]
public sealed class RegenerateStorageAccountKeyCommand(HttpClient httpClient)
    : TopazHttpCommand<RegenerateStorageAccountKeyCommand.RegenerateStorageAccountKeyCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, RegenerateStorageAccountKeyCommandSettings settings)
    {
        AnsiConsole.WriteLine($"Regenerating storage account key '{settings.KeyName}'...");

        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Storage/storageAccounts/{settings.AccountName}/regenerateKey";
        var (success, body) = await PostAsync(url, new { keyName = settings.KeyName });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, RegenerateStorageAccountKeyCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Storage account name can't be null.");

        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group can't be null.");

        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        if (string.IsNullOrEmpty(settings.KeyName))
            return ValidationResult.Error("Key name can't be null.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class RegenerateStorageAccountKeyCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Storage account name.", required: true)]
        [CommandOption("-n|--account-name")] public string? AccountName { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")] public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")] public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) The key to regenerate (key1 or key2).", required: true)]
        [CommandOption("-k|--key-name")] public string? KeyName { get; set; }
    }
}
