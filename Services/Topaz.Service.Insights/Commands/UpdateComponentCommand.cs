using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Insights.Commands;

[UsedImplicitly]
[CommandDefinition("insights component update", "application-insights", "Updates tags or properties of an Application Insights component.")]
[CommandExample("Updates the retention period of an Application Insights component",
    "topaz insights component update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-appinsights\" \\\n    --resource-group \"rg-local\" \\\n    --retention-in-days 180")]
internal sealed class UpdateComponentCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<UpdateComponentCommand.UpdateComponentCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, UpdateComponentCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/microsoft.insights/components/{settings.Name}";
        var (success, body) = await PatchAsync(url, new
        {
            properties = new
            {
                RetentionInDays = settings.RetentionInDays,
                PublicNetworkAccessForIngestion = settings.PublicNetworkAccessForIngestion
            }
        });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, UpdateComponentCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("Component name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("Resource group name can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class UpdateComponentCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) component name")]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }

        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("Data retention in days")]
        [CommandOption("--retention-in-days")]
        public int? RetentionInDays { get; set; }

        [CommandOptionDefinition("Public network access for ingestion (Enabled/Disabled)")]
        [CommandOption("--public-network-access-for-ingestion")]
        public string? PublicNetworkAccessForIngestion { get; set; }
    }
}
