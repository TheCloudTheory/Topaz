using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Sql.Commands;

[UsedImplicitly]
[CommandDefinition("sql create", "sql-server", "Creates or updates an Azure SQL Server.")]
[CommandExample("Creates a new SQL Server",
    "topaz sql create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-sql-server\" \\\n    --location \"westeurope\" \\\n    --resource-group \"rg-local\" \\\n    --admin-user \"sqladmin\" \\\n    --admin-password \"SqlAdmin1234!@#\"")]
internal sealed class CreateSqlServerCommand(HttpClient httpClient)
    : TopazHttpCommand<CreateSqlServerCommand.CreateSqlServerCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateSqlServerCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Sql/servers/{settings.Name}";
        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            properties = new
            {
                administratorLogin = settings.AdminUser,
                administratorLoginPassword = settings.AdminPassword,
                version = "12.0"
            }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateSqlServerCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("SQL server name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.Location))
            return ValidationResult.Error("Location can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        if (string.IsNullOrEmpty(settings.AdminUser))
            return ValidationResult.Error("Administrator login can't be null.");
        if (string.IsNullOrEmpty(settings.AdminPassword))
            return ValidationResult.Error("Administrator login password can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateSqlServerCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) SQL server name.", required: true)]
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

        [CommandOptionDefinition("(Required) Administrator login username.", required: true)]
        [CommandOption("-u|--admin-user")]
        public string? AdminUser { get; set; }

        [CommandOptionDefinition("(Required) Administrator login password.", required: true)]
        [CommandOption("-p|--admin-password")]
        public string? AdminPassword { get; set; }
    }
}
