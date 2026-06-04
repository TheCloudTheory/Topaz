using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.CosmosDb.Commands;

[UsedImplicitly]
[CommandDefinition("cosmosdb sql-database delete", "cosmos-db", "Deletes a SQL database in an Azure Cosmos DB account.")]
[CommandExample("Delete a SQL database", "topaz cosmosdb sql-database delete \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"my-cosmos-account\" \\\n    --database-name \"my-database\"")]
public sealed class DeleteSqlDatabaseCommand(HttpClient httpClient)
    : TopazHttpCommand<DeleteSqlDatabaseCommand.DeleteSqlDatabaseCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, DeleteSqlDatabaseCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/{settings.AccountName}/sqlDatabases/{settings.DatabaseName}";
        if (!await DeleteAsync(url)) return 1;
        AnsiConsole.WriteLine($"SQL database '{settings.DatabaseName}' deleted from Cosmos DB account '{settings.AccountName}'.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteSqlDatabaseCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.AccountName))
            return ValidationResult.Error("Cosmos DB account name can't be null.");
        if (string.IsNullOrEmpty(settings.DatabaseName))
            return ValidationResult.Error("SQL database name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class DeleteSqlDatabaseCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) Cosmos DB account name.", required: true)]
        [CommandOption("-a|--account-name")]
        public string? AccountName { get; set; }

        [CommandOptionDefinition("(Required) SQL database name.", required: true)]
        [CommandOption("-n|--database-name")]
        public string? DatabaseName { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
