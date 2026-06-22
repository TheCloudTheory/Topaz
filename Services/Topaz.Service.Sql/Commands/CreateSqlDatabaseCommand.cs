using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Sql.Commands;

[UsedImplicitly]
[CommandDefinition("sql db create", "sql-database", "Creates or updates an Azure SQL Database.")]
[CommandExample("Creates a new SQL Database",
    "topaz sql db create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --server \"my-sql-server\" \\\n    --name \"my-database\" \\\n    --resource-group \"rg-local\" \\\n    --location \"westeurope\"")]
internal sealed class CreateSqlDatabaseCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateSqlDatabaseCommand.CreateSqlDatabaseCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateSqlDatabaseCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}" +
                  $"/providers/Microsoft.Sql/servers/{settings.Server}/databases/{settings.Name}";
        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            properties = new { }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateSqlDatabaseCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;
        if (string.IsNullOrEmpty(settings.Server))
            return ValidationResult.Error("SQL server name can't be null.");
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("SQL database name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.Location))
            return ValidationResult.Error("Location can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateSqlDatabaseCommandSettings : CommandSettings
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

        [CommandOptionDefinition("(Required) Azure region.", required: true)]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }
    }
}
