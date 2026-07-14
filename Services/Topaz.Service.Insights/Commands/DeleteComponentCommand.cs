using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Insights.Commands;

[UsedImplicitly]
[CommandDefinition("insights component delete", "application-insights", "Deletes an Application Insights component.")]
[CommandExample("Deletes an Application Insights component",
    "topaz insights component delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --name \"my-appinsights\" \\\n    --resource-group \"rg-local\"")]
internal sealed class DeleteComponentCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<DeleteComponentCommand.DeleteComponentCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, DeleteComponentCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/microsoft.insights/components/{settings.Name}";
        if (!await DeleteAsync(url)) return 1;
        AnsiConsole.WriteLine($"Component '{settings.Name}' deleted.");
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, DeleteComponentCommandSettings settings)
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
    public sealed class DeleteComponentCommandSettings : CommandSettings
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
    }
}
