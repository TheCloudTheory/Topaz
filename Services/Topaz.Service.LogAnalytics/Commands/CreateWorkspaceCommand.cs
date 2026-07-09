using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.LogAnalytics.Commands;

[UsedImplicitly]
[CommandDefinition("loganalytics create", "log-analytics", "Creates or updates an Azure Log Analytics workspace.")]
[CommandExample("Creates a new Log Analytics workspace",
    "topaz loganalytics create --subscription-id 36a28ebb-9370-46d8-981c-84efe0204800 \\\n    --name \"my-workspace\" \\\n    --location \"westeurope\" \\\n    --resource-group \"rg-local\"")]
internal sealed class CreateWorkspaceCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateWorkspaceCommand.Settings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.OperationalInsights/workspaces/{settings.Name}";
        var (success, body) = await PutAsync(url, new
        {
            location = settings.Location,
            properties = new
            {
                sku = settings.Sku == null ? null : new { name = settings.Sku },
                retentionInDays = settings.RetentionInDays
            }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, Settings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Workspace name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.Location))
            return ValidationResult.Error("Location can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class Settings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Workspace name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) Location.", required: true)]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Optional) SKU name (e.g. PerGB2018, Free, CapacityReservation).", required: false)]
        [CommandOption("--sku")]
        public string? Sku { get; set; }

        [CommandOptionDefinition("(Optional) Retention in days.", required: false)]
        [CommandOption("--retention-in-days")]
        public int? RetentionInDays { get; set; }
    }
}
