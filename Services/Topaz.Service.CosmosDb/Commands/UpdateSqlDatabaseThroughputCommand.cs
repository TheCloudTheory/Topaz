using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.CosmosDb.Commands;

[UsedImplicitly]
[CommandDefinition("cosmosdb sql-database update-throughput", "cosmos-db", "Updates the throughput settings for a SQL database in an Azure Cosmos DB account.")]
[CommandExample("Update throughput for a SQL database", "topaz cosmosdb sql-database update-throughput \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --account-name \"my-cosmos-account\" \\\n    --database-name \"my-database\" \\\n    --throughput 800")]
public sealed class UpdateSqlDatabaseThroughputCommand(HttpClient httpClient)
    : TopazHttpCommand<UpdateSqlDatabaseThroughputCommand.UpdateSqlDatabaseThroughputCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, UpdateSqlDatabaseThroughputCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/{settings.AccountName}/sqlDatabases/{settings.DatabaseName}/throughputSettings/default";
        var body = new
        {
            properties = new
            {
                resource = new { throughput = settings.Throughput }
            }
        };
        var (success, responseBody) = await PutAsync(url, body);
        if (!success) return 1;
        AnsiConsole.WriteLine(responseBody);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, UpdateSqlDatabaseThroughputCommandSettings settings)
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
        if (settings.Throughput <= 0)
            return ValidationResult.Error("Throughput must be a positive number.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class UpdateSqlDatabaseThroughputCommandSettings : CommandSettings
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

        [CommandOptionDefinition("(Required) Manual throughput in RU/s.", required: true)]
        [CommandOption("--throughput")]
        public int Throughput { get; set; }
    }
}
