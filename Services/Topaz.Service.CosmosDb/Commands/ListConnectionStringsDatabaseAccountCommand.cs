using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.CosmosDb.Commands;

[UsedImplicitly]
[CommandDefinition("cosmosdb account list-connection-strings", "cosmos-db", "Lists the connection strings for an Azure Cosmos DB account.")]
[CommandExample("List connection strings for a Cosmos DB account", "topaz cosmosdb account list-connection-strings \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"my-cosmos-account\"")]
public sealed class ListConnectionStringsDatabaseAccountCommand(HttpClient httpClient)
    : TopazHttpCommand<ListConnectionStringsDatabaseAccountCommand.ListConnectionStringsDatabaseAccountCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ListConnectionStringsDatabaseAccountCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/{settings.Name}/listConnectionStrings";
        var (success, body) = await PostAsync(url, new { });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListConnectionStringsDatabaseAccountCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Cosmos DB account name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListConnectionStringsDatabaseAccountCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) Cosmos DB account name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
