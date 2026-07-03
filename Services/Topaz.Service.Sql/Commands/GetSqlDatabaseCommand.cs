using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Sql.Commands;

[UsedImplicitly]
[CommandDefinition("sql db show", "sql-database", "Gets an Azure SQL Database.")]
[CommandExample("Gets a SQL Database",
    "topaz sql db show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --server \"my-sql-server\" \\\n    --name \"my-database\" \\\n    --resource-group \"rg-local\"")]
internal sealed class GetSqlDatabaseCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<GetSqlDatabaseCommand.GetSqlDatabaseCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, GetSqlDatabaseCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}" +
                  $"/providers/Microsoft.Sql/servers/{settings.Server}/databases/{settings.Name}";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, GetSqlDatabaseCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Server))
            return ValidationResult.Error("SQL server name can't be null.");
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("SQL database name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GetSqlDatabaseCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) SQL server name.", required: true)]
        [CommandOption("--server")]
        public string? Server { get; set; }

        [CommandOptionDefinition("(Required) SQL database name.", required: true)]
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
