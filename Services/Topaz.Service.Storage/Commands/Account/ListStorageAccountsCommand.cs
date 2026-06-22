using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Storage.Commands;

[UsedImplicitly]
[CommandDefinition("storage account list", "azure-storage/account", "Lists Azure Storage accounts.")]
[CommandExample("List all accounts in a subscription", "topaz storage account list \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\"")]
[CommandExample("List accounts in a resource group", "topaz storage account list \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\"")]
public sealed class ListStorageAccountsCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<ListStorageAccountsCommand.ListStorageAccountsCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ListStorageAccountsCommandSettings settings)
    {
        AnsiConsole.WriteLine("Listing storage accounts...");

        var url = !string.IsNullOrEmpty(settings.ResourceGroup)
            ? $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Storage/storageAccounts"
            : $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.Storage/storageAccounts";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListStorageAccountsCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListStorageAccountsCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("Resource group name (filters to accounts in this group when specified).")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
