using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.ContainerRegistry.Commands;

[UsedImplicitly]
[CommandDefinition("acr list", "container-registry", "Lists Azure Container Registries.")]
[CommandExample("List registries in a resource group", "topaz acr list \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\" \\\n    --resource-group \"my-rg\"")]
[CommandExample("List all registries in a subscription", "topaz acr list \\\n    --subscription-id \"00000000-0000-0000-0000-000000000000\"")]
public sealed class ListContainerRegistriesCommand(HttpClient httpClient, DefaultsProvider provider)
    : TopazHttpCommand<ListContainerRegistriesCommand.ListContainerRegistriesCommandSettings>(httpClient)
{

    public override async Task<int> ExecuteAsync(CommandContext context, ListContainerRegistriesCommandSettings settings)
    {
        var url = !string.IsNullOrEmpty(settings.ResourceGroup)
            ? $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/resourceGroups/{settings.ResourceGroup}/providers/Microsoft.ContainerRegistry/registries"
            : $"{ArmBaseUrl}/subscriptions/{settings.SubscriptionId}/providers/Microsoft.ContainerRegistry/registries";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, ListContainerRegistriesCommandSettings settings)
    {
        var defaults = provider.LoadDefaults();
        settings.SubscriptionId ??= defaults.SubscriptionId;
        if (string.IsNullOrEmpty(settings.SubscriptionId))
            return ValidationResult.Error("Subscription ID can't be null.");

        if (!Guid.TryParse(settings.SubscriptionId, out _))
            return ValidationResult.Error("Subscription ID must be a valid GUID.");

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class ListContainerRegistriesCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.")]
        [CommandOption("-s|--subscription-id")]
        public string? SubscriptionId { get; set; }

        [CommandOptionDefinition("Resource group name. Omit to list across the entire subscription.")]
        [CommandOption("-g|--resource-group")]
        public string? ResourceGroup { get; set; }
    }
}
