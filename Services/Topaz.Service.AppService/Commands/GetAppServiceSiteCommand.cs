using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.AppService.Commands;

[UsedImplicitly]
[CommandDefinition("appservice site get", "app-service", "Gets a Web App or Function App.")]
[CommandExample("Get a web app", "topaz appservice site get \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"my-site\"")]
public sealed class GetAppServiceSiteCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<GetAppServiceSiteCommand.GetAppServiceSiteCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, GetAppServiceSiteCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Web/sites/{settings.Name}";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, GetAppServiceSiteCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("App Service Site name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("App Service Site resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("App Service Site subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("App Service Site subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GetAppServiceSiteCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) App Service Site name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
