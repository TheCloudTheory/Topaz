using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.AppService.Commands;

[UsedImplicitly]
[CommandDefinition("appservice site create", "app-service", "Creates or updates a Web App or Function App (Microsoft.Web/sites).")]
[CommandExample("Create a web app", "topaz appservice site create \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"my-site\" \\\n    --location \"westeurope\"")]
public sealed class CreateAppServiceSiteCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<CreateAppServiceSiteCommand.CreateAppServiceSiteCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateAppServiceSiteCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Web/sites/{settings.Name}";
        var body = new
        {
            location = settings.Location,
            kind = settings.Kind,
            properties = new { serverFarmId = settings.Plan }
        };
        var (success, responseBody) = await PutAsync(url, body);
        if (!success) return 1;
        AnsiConsole.WriteLine(responseBody);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, CreateAppServiceSiteCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        settings.Location ??= defaults.Location;
        if (string.IsNullOrEmpty(settings.Name))
            return ValidationResult.Error("App Service Site name can't be null.");
        if (string.IsNullOrEmpty(settings.ResourceGroup))
            return ValidationResult.Error("App Service Site resource group can't be null.");
        if (string.IsNullOrEmpty(settings.Location))
            return ValidationResult.Error("App Service Site location can't be null.");
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("App Service Site subscription ID can't be null.");
        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("App Service Site subscription ID must be a valid GUID.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class CreateAppServiceSiteCommandSettings : CommandSettings
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

        [CommandOptionDefinition("(Required) Azure region.", required: true)]
        [CommandOption("-l|--location")]
        public string? Location { get; set; }

        [CommandOptionDefinition("(Optional) Site kind: app, functionapp, functionapp,linux. Defaults to app.", required: false)]
        [CommandOption("--kind")]
        public string Kind { get; set; } = "app";

        [CommandOptionDefinition("(Optional) Resource ID of the App Service Plan to associate with this site.", required: false)]
        [CommandOption("--plan")]
        public string? Plan { get; set; }
    }
}
