using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.AppService.Commands;

[UsedImplicitly]
[CommandDefinition("appservice plan get", "app-service", "Gets an App Service Plan.")]
[CommandExample("Get a plan", "topaz appservice plan get \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"my-plan\"")]
public sealed class GetAppServicePlanCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<GetAppServicePlanCommand.GetAppServicePlanCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, GetAppServicePlanCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Web/serverfarms/{settings.Name}";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, GetAppServicePlanCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("App Service Plan name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("App Service Plan resource group can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("App Service Plan subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("App Service Plan subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class GetAppServicePlanCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; } = null!;

        [CommandOptionDefinition("(Required) App Service Plan name.", required: true)]
        [CommandOption("-n|--name")]
        public string? Name { get; set; }

        [CommandOptionDefinition("(Required) Resource group name.", required: true)]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
