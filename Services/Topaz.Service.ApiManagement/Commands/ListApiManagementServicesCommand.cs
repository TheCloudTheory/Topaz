using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ApiManagement.Commands;

[UsedImplicitly]
[CommandDefinition("apim list", "api-management", "Lists Azure API Management services in a subscription or resource group.")]
[CommandExample("Lists API Management services in a resource group",
    "topaz apim list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \\\n    --resource-group \"rg-local\"")]
internal sealed class ListApiManagementServicesCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<ListApiManagementServicesCommand.ListApiManagementServicesCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ListApiManagementServicesCommandSettings settings, CancellationToken cancellationToken)
    {
        string url;
        if (!string.IsNullOrWhiteSpace(settings.ResourceGroup))
            url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ApiManagement/service";
        else
            url = $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.ApiManagement/service";

        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, ListApiManagementServicesCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        settings.ResourceGroup ??= defaults.ResourceGroup;
        return string.IsNullOrEmpty(settings.SubscriptionId)
            ? ValidationResult.Error("Subscription ID can't be null.")
            : base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListApiManagementServicesCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) subscription ID")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("(Optional) filter by resource group name")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
