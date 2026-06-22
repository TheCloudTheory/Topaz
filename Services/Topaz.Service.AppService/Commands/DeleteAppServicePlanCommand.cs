using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.AppService.Commands;

[UsedImplicitly]
[CommandDefinition("appservice plan delete", "app-service", "Deletes an App Service Plan.")]
[CommandExample("Delete a plan", "topaz appservice plan delete \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"rg-local\" \\\n    --name \"my-plan\"")]
public sealed class DeleteAppServicePlanCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<DeleteAppServicePlanCommand.DeleteAppServicePlanCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, DeleteAppServicePlanCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.Web/serverfarms/{settings.Name}";
        if (!await DeleteAsync(url)) return 1;
        AnsiConsole.WriteLine($"App Service Plan '{settings.Name}' deleted.");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, DeleteAppServicePlanCommandSettings settings)
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
    public sealed class DeleteAppServicePlanCommandSettings : CommandSettings
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
