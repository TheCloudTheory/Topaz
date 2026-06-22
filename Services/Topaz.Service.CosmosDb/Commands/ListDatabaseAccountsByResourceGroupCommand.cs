using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.CosmosDb.Commands;

[UsedImplicitly]
[CommandDefinition("cosmosdb account list", "cosmos-db", "Lists Azure Cosmos DB accounts in a resource group.")]
[CommandExample("List Cosmos DB accounts in a resource group", "topaz cosmosdb account list \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\"")]
public sealed class ListDatabaseAccountsByResourceGroupCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<ListDatabaseAccountsByResourceGroupCommand.ListDatabaseAccountsByResourceGroupCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ListDatabaseAccountsByResourceGroupCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListDatabaseAccountsByResourceGroupCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListDatabaseAccountsByResourceGroupCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
