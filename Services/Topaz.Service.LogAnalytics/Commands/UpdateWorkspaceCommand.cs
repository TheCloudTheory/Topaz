using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.LogAnalytics.Commands;

[UsedImplicitly]
[CommandDefinition("loganalytics update", "log-analytics", "Updates an Azure Log Analytics workspace (tags, SKU, retentionInDays).")]
[CommandExample("Updates a Log Analytics workspace's retention",
    "topaz loganalytics update --subscription-id 36a28ebb-9370-46d8-981c-84efe0204800 \\\n    --name \"my-workspace\" \\\n    --resource-group \"rg-local\" \\\n    --retention-in-days 60")]
internal sealed class UpdateWorkspaceCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<UpdateWorkspaceCommand.Settings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.OperationalInsights/workspaces/{settings.Name}";

        var tags = settings.Tags?
            .Select(t => t.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1]);

        var props = new Dictionary<string, object?>();
        if (settings.RetentionInDays.HasValue) props["retentionInDays"] = settings.RetentionInDays.Value;
        if (settings.Sku != null) props["sku"] = new { name = settings.Sku };

        var body = new Dictionary<string, object?>();
        if (tags != null) body["tags"] = tags;
        if (props.Count > 0) body["properties"] = props;

        var (success, responseBody) = await PatchAsync(url, body);
        if (!success) return 1;
        AnsiConsole.WriteLine(responseBody);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, Settings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Workspace name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
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

        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Optional) Tags in key=value format.", required: false)]
        [CommandOption("--tags")]
        public string[]? Tags { get; set; }

        [CommandOptionDefinition("(Optional) SKU name (e.g. PerGB2018, Free, CapacityReservation).", required: false)]
        [CommandOption("--sku")]
        public string? Sku { get; set; }

        [CommandOptionDefinition("(Optional) Retention in days.", required: false)]
        [CommandOption("--retention-in-days")]
        public int? RetentionInDays { get; set; }
    }
}
